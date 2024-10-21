using FluentValidation.Results;
using NerdStore.Core.DomainObjects;

namespace NerdStore.Vendas.Domain;

public partial class Pedido : Entity, IAggregateRoot
{
    public decimal ValorTotal { get; private set; }
    public decimal Desconto { get; private set; }

    public Guid ClienteId { get; private set; }

    public PedidoStatus PedidoStatus { get; private set; }

    private readonly List<PedidoItem> _pedidoItems;
    public IReadOnlyCollection<PedidoItem> PedidoItems => _pedidoItems;

    public bool VoucherUtilizado { get; private set; }
    public Voucher Voucher { get; private set; }

    protected Pedido()
    {
        _pedidoItems = new List<PedidoItem>();
    }

    public ValidationResult AplicarVoucher(Voucher voucher)
    {
        var result = voucher.ValidarSeAplicavel();
        if (!result.IsValid) return result;

        Voucher = voucher;
        VoucherUtilizado = true;

        CalcularValorTotalDesconto();

        return result;
    }

    public void CalcularValorTotalDesconto()
    {
        if (!VoucherUtilizado) return;
        decimal desconto = 0;
        var valor = ValorTotal;

        if (Voucher.TipoDescontoVoucher == TipoDescontoVoucher.Valor)
        {
            if (Voucher.ValorDesconto.HasValue)
            {
                desconto = Voucher.ValorDesconto.Value;
                valor -= desconto;
            }
        }
        else
        {
            if (Voucher.PercentualDesconto.HasValue)
            {
                desconto = (ValorTotal * Voucher.PercentualDesconto.Value) / 100;
                valor -= desconto;
            }
        }

        ValorTotal = valor < 0 ? 0 : valor;
        Desconto = desconto;
    }

    public bool PedidoItemExistente(PedidoItem item)
    {
        return _pedidoItems.Any(p => p.ProdutoId == item.ProdutoId);
    }

    private void ValidarPedidoItemInexistente(PedidoItem item)
    {
        if (!PedidoItemExistente(item)) throw new DomainException("O item não pertence ao pedido");
    }

    public void AdicionarItem(PedidoItem pedidoItem)
    {
        if (pedidoItem.Quantidade > 15) throw new DomainException("Máximo de unidades: 15 por produto");
        if (pedidoItem.Quantidade < 1) throw new DomainException("Mínimo de unidades: 1 por produto");
        var itemExistente = _pedidoItems.FirstOrDefault(p => p.ProdutoId == pedidoItem.ProdutoId);

        if (itemExistente != null)
        {
            var quantidadeItens = pedidoItem.Quantidade;
            if (quantidadeItens + itemExistente.Quantidade > 15) throw new DomainException("Máximo de unidades: 15 por produto");
            itemExistente.AdicionarUnidades(pedidoItem.Quantidade);
            pedidoItem = itemExistente;

            _pedidoItems.Remove(itemExistente);
        }

        _pedidoItems.Add(pedidoItem);
        CalcularValorPedido();
    }

    public void AtualizarItem(PedidoItem pedidoItem)
    {
        if (!PedidoItemExistente(pedidoItem)) throw new DomainException("O item não existe no pedido");
        if (pedidoItem.Quantidade > 15) throw new DomainException("Máximo de unidades: 15 por produto");
        if (pedidoItem.Quantidade < 1) throw new DomainException("Mínimo de unidades: 1 por produto");

        var itemExistente = PedidoItems.FirstOrDefault(p => p.ProdutoId == pedidoItem.ProdutoId);

        _pedidoItems.Remove(itemExistente);
        _pedidoItems.Add(pedidoItem);

        CalcularValorPedido();
    }

    private void CalcularValorPedido()
    {
        ValorTotal = PedidoItems.Sum(i => i.CalcularValor());
        CalcularValorTotalDesconto();
    }

    public void TornarRascunho()
    {
        PedidoStatus = PedidoStatus.Rascunho;
    }

    public void RemoverItem(PedidoItem pedidoItem)
    {
        ValidarPedidoItemInexistente(pedidoItem);
        _pedidoItems.Remove(pedidoItem);
        CalcularValorPedido();
    }

    public static class PedidoFactory
    {
        public static Pedido NovoPedidoRascunho(Guid clienteId)
        {
            var pedido = new Pedido
            {
                ClienteId = clienteId,
            };

            pedido.TornarRascunho();
            return pedido;
        }
    }
}