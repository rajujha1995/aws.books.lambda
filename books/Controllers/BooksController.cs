using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.S3.Model;
using books.Model;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace books.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly IDynamoDBContext _context;
    private readonly string _bucketName = "";

    public BooksController(IDynamoDBContext context, IAmazonS3 s3Client)
    {
        _context = context;
        _s3Client = s3Client;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var book = await _context.LoadAsync<Book>(id);
        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _context.ScanAsync<Book>(default).GetRemainingAsync();
        return Ok(books);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Book request)
    {
        var book = await _context.LoadAsync<Book>(request.Id);
        if (book != null) return BadRequest($"Book with Id {request.Id} Already Exists");
        await _context.SaveAsync(request);
        return Ok(request);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.LoadAsync<Book>(id);
        if (book == null) return NotFound();
        await _context.DeleteAsync(book);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Update(Book request)
    {
        var book = await _context.LoadAsync<Book>(request.Id);
        if (book == null) return NotFound();
        await _context.SaveAsync(request);
        return Ok(request);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFileToS3Async(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (string.IsNullOrEmpty(image.FileName))
        {
            return BadRequest("Image name is required.");
        }
        var doesS3BucketExist = await _s3Client.DoesS3BucketExistAsync(_bucketName);
        if (!doesS3BucketExist) return NotFound($"Bucket {_bucketName} does not exist.");

        var putObjectRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = image.FileName,
            InputStream = image.OpenReadStream()
        };

        putObjectRequest.Metadata.Add("Content-Type", image.ContentType);
        var imageUploadStatus = await _s3Client.PutObjectAsync(putObjectRequest);

        if (imageUploadStatus.HttpStatusCode == HttpStatusCode.OK)
        {
            var listObjectsV2Request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = image.FileName
            };
            var listObjects = await _s3Client.ListObjectsV2Async(listObjectsV2Request);

            var s3ObjectsDetails = listObjects.S3Objects.Select(s =>
            {
                var urlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = s.Key,
                    Expires = DateTime.UtcNow.AddYears(1)
                };
                return new HandleS3Images
                {
                    ImageName = s.Key,
                    ImagePresignedUrl = _s3Client.GetPreSignedURL(urlRequest),
                };
            });

            return Ok(s3ObjectsDetails);
        }

        return StatusCode((int)HttpStatusCode.InternalServerError, "Error uploading file to S3");
    }
}
