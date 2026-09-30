namespace Wallet.Tenant;

public enum Categoria
{
    Adulto,
    Juvenil,
    Nino
}

/// <summary>Valores de texto de la categoría en la credencial y en la API (<c>adulto</c>, <c>juvenil</c>, <c>niño</c>).</summary>
public static class CategoriaText
{
    public static string ToText(this Categoria categoria) => categoria switch
    {
        Categoria.Adulto => "adulto",
        Categoria.Juvenil => "juvenil",
        Categoria.Nino => "niño",
        _ => throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoría inválida.")
    };

    public static bool TryParse(string? text, out Categoria categoria)
    {
        switch (text)
        {
            case "adulto":
                categoria = Categoria.Adulto;
                return true;
            case "juvenil":
                categoria = Categoria.Juvenil;
                return true;
            case "niño":
                categoria = Categoria.Nino;
                return true;
            default:
                categoria = default;
                return false;
        }
    }
}
