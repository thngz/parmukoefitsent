namespace Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public decimal AlcContent { get; set; }
    public decimal Coefficient { get; set; }
    public string ProductUrl { get; set; }

    public int StoreId { get; set; }
    public Store? Store { get; set; }
}
