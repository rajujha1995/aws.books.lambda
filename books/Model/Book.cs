using Amazon.DynamoDBv2.DataModel;

namespace books.Model;

[DynamoDBTable("books")]
public class Book
{
    [DynamoDBHashKey("id")]
    public int? Id { get; set; }
    [DynamoDBProperty("name")]
    public string? Name { get; set; }
    [DynamoDBProperty("author")]
    public string? Author { get; set; }
    [DynamoDBProperty("description")]
    public string? Description { get; set; }
    [DynamoDBProperty("price")]
    public decimal Price { get; set; }
    [DynamoDBProperty("imageurl")]
    public string? Imageurl { get; set; }

}

public class HandleS3Images
{
    public string? ImageName { get; set; }
    public string? ImagePresignedUrl { get; set; }
}
