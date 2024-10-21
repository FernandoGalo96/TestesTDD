namespace NerdStore.Vendas.Domain;

public class PedidoItem
{
    public PedidoItem(Guid pedidoId, string produtoNome, int quantidade, decimal valorUnitario)
    {
        ProdutoId = pedidoId;
        ProdutoNome = produtoNome;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    public Guid ProdutoId { get; private set; }

    public string ProdutoNome { get; private set; }
    public int Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }

    internal void AdicionarUnidades(int quantidades)
    {
        Quantidade += quantidades;
    }

    internal decimal CalcularValor()
    {
        return Quantidade * ValorUnitario;
    }
}