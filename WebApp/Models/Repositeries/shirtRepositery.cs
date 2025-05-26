namespace WebApp.Models.Repositeries;

public static class ShirtRepositery
{
    private static readonly List<Shirt> Shirts =
    [
        new Shirt
        {
            ShirtId = 1,
            Brand = "hamada",
            Color = "Blue",
            Gender = "Men",
            Price = 30,
            Size = 10,
        },
        new() {
            ShirtId = 2,
            Brand = "My brand",
            Color = "Black",
            Gender = "Men",
            Price = 35,
            Size = 12,
        },
        new() {
            ShirtId = 3,
            Brand = "your brand",
            Color = "Pink",
            Gender = "Women",
            Price = 28,
            Size = 8,
        },
        new Shirt
        {
            ShirtId = 4,
            Brand = "your brand",
            Color = "yello",
            Gender = "Women",
            Price = 30,
            Size = 9,
        },
    ];

    public static List<Shirt> GetShirts()
    {
        return Shirts;
    }

    public static bool ShirtExists(int id)
    {
        return Shirts.Any(x => x.ShirtId == id);
    }

    public static Shirt? GetShirtById(int id)
    {
        return Shirts.FirstOrDefault(x => x.ShirtId == id);
    }

    public static Shirt GetShirtByProperties(
        string? brand,
        string? gender,
        string? color,
        int? size
    )
    {
        return Shirts.FirstOrDefault(x =>
  !string.IsNullOrWhiteSpace(brand)
&& !string.IsNullOrWhiteSpace(x.Brand)
&& x.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase)
&& !string.IsNullOrWhiteSpace(gender)
&& !string.IsNullOrWhiteSpace(x.Gender)
&& x.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)
&& !string.IsNullOrWhiteSpace(color)
&& !string.IsNullOrWhiteSpace(x.Color)
&& x.Color.Equals(color, StringComparison.OrdinalIgnoreCase)
&& size.HasValue
&& x.Size.HasValue
&& size.Value == x.Size.Value
);
    }

    public static void AddShirt(Shirt shirt)
    {
        int maxId = Shirts.Max(x => x.ShirtId);
        shirt.ShirtId = maxId + 1;
        Shirts.Add(shirt);
    }

    public static void UpdateShirt(Shirt shirt)
    {
        var shirtToUpdate = Shirts.First(x => x.ShirtId == shirt.ShirtId);
        shirtToUpdate.Brand = shirt.Brand;
        shirtToUpdate.Price = shirt.Price;
        shirtToUpdate.Size = shirt.Size;
        shirtToUpdate.Gender = shirt.Gender;
        shirtToUpdate.Color = shirt.Color;

    }
    public static void DeleteShirt(int shirtId)
    {
        var shirt = GetShirtById(shirtId);
        if (shirt != null)
        {
            Shirts.Remove(shirt);
        }


    }
}