using API.Web.Attributes;
using DMP.BL.Models;
using DMP.BL.Models.ProductCreation;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class ProductManagerController(
    ILogger<ProductManagerController> logger,
    IProductManagerService productManagerService) : ControllerCustom(logger)
{
    private const int MaxProductImageSize = 2 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<NewProductRequest>> GetProduct(int productId)
    {
        try
        {
            var getNewProductResponse = await productManagerService.GetProduct(UserId, productId);

            if (getNewProductResponse.Status != GetNewProductRequestStatus.Success)
            {
                return NotFound();
            }

            return Ok(getNewProductResponse.Product!);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get product {ProductId}", productId);
            return StatusCode(500);
        }
    }

    [HttpGet("view")]
    public async Task<ActionResult<ProductView>> GetProductView(int productId)
    {
        try
        {
            var productView = await productManagerService.GetProductView(UserId, productId);

            if (productView is null)
            {
                return NotFound();
            }

            return Ok(productView);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get product view {ProductId}", productId);
            return StatusCode(500);
        }
    }

    [HttpGet("getproductdataitems")]
    public async Task<ActionResult<GetProductDataItemsResponse>> GetProductDataItems(int productId)
    {
        try
        {
            var result = await productManagerService.GetProductDataItems(UserId, productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get data items of product {ProductId}", productId);
            return StatusCode(500);
        }
    }

    [RequestSizeLimit(21 * 1024 * 1024)]
    [HttpPost("product")]
    [ExtractLocale]
    public async Task<ActionResult<CreateOrUpdateProductResponse>> CreateOrUpdateProduct([FromBody] NewProductRequest newProduct,
        [HideFromOpenApi] string locale = "")
    {
        var validationResult = newProduct.Validate();

        if (!validationResult.IsValid)
        {
            return BadRequest();
        }

        try
        {
            var result = await productManagerService.CreateOrUpdateNewProduct(UserId, newProduct, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create or update product");
            return StatusCode(500);
        }
    }

    [RequestSizeLimit(MaxProductImageSize)]
    [HttpPost("uploadproductimage")]
    public async Task<ActionResult<UploadProductImageResponse>> UploadProductImage([FromForm] IFormCollection formData)
    {
        var productIdStr = formData["productId"].ToString();

        if (string.IsNullOrEmpty(productIdStr) || !int.TryParse(productIdStr, out var productId))
        {
            return BadRequest();
        }

        var file = formData.Files.FirstOrDefault();

        if (file is null)
        {
            return BadRequest("Image is missed.");
        }

        if (!Constants.AllowedProductImagesMimeTypes.Contains(file.ContentType.ToLower()))
        {
            return new UploadProductImageResponse
            {
                Status = CreateOrUpdateProductStatus.InvalidImage
            };
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        if (!Constants.AllowedProductImagesExtensions.Contains(fileExtension))
        {
            return new UploadProductImageResponse
            {
                Status = CreateOrUpdateProductStatus.InvalidImage
            };
        }

        var productImgFile = new NewProductFile
        {
            Extension = fileExtension
        };

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);

            if (memoryStream.Length >= MaxProductImageSize)
            {
                return new UploadProductImageResponse
                {
                    Status = CreateOrUpdateProductStatus.InvalidImage
                };
            }

            productImgFile.Data = memoryStream.ToArray();
        }

        try
        {
            var result = await productManagerService.UploadProductImage(UserId, productImgFile, productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to upload image for product {ProductId}", productId);
            return StatusCode(500);
        }
    }

    [HttpPost("setproductimages")]
    public async Task<ActionResult<bool>> SetProductImages([FromBody] SetProductImagesRequest request)
    {
        try
        {
            if (!request.Validate().IsValid)
            {
                Logger.LogError("Invalid SetProductImagesRequest: {Request}", request);
                return BadRequest();
            }

            var result = await productManagerService.SetProductImages(UserId, request.NewProductImages, request.ProductId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set images for product {ProductId}", request.ProductId);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("getcategories")]
    [ExtractLocale]
    public async Task<ActionResult<ProductCategory[]>> GetCategories(
        [HideFromOpenApi] string locale = "")
    {
        const int menuId = 1;

        try
        {
            var categories = await productManagerService.GetProductCategories(locale, menuId);

            if (!categories.Any())
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = $"Resource with menu ID {menuId} was not found."
                });
            }

            return Ok(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get product categories for menu {MenuId}", menuId);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("getcategoryfeatures")]
    [ExtractLocale]
    public async Task<ActionResult<CategoryFeature[]>> GetCategoryFeatures(int categoryId,
        [HideFromOpenApi] string locale = "en")
    {
        try
        {
            var features = await productManagerService.GetCategoryFeatures(categoryId, locale);

            return Ok(features);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get features of category {CategoryId}", categoryId);
            return StatusCode(500);
        }
    }

    [HttpPost("generateuploadurl")]
    public async Task<ActionResult<GenerateUploadUrlResponse>> GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request)
    {
        try
        {
            var validationResult = request.Validate();
            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid GenerateUploadUrlRequest: {Request}. Error: {ErrorMsg}", request,
                    validationResult.ErrorMsg);
                return BadRequest();
            }

            var result = await productManagerService.GenerateUploadUrl(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate upload URL");
            return StatusCode(500);
        }
    }

    [HttpPost("delete-file")]
    public async Task<ActionResult<bool>> DeleteProductFile([FromBody] SetProductFileUploadedRequest request)
    {
        try
        {
            if (!request.Validate().IsValid)
            {
                Logger.LogError("Invalid SetProductFileUploadedRequest: {Request}", request);
                return BadRequest();
            }

            var result = await productManagerService.DeleteProductFile(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete product file");
            return StatusCode(500);
        }
    }

    [HttpPost("setproductfileuploaded")]
    public async Task<ActionResult<bool>> SetProductFileUploaded([FromBody] SetProductFileUploadedRequest request)
    {
        try
        {
            if (!request.Validate().IsValid)
            {
                Logger.LogError("Invalid SetProductFileUploadedRequest: {Request}", request);
                return BadRequest();
            }

            var result = await productManagerService.SetProductFileUploaded(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to mark product file as uploaded");
            return StatusCode(500);
        }
    }

    [HttpPost("setproductfiles")]
    public async Task<ActionResult<bool>> SetProductFiles([FromBody] SetProductFilesRequest request)
    {
        try
        {
            var validationResult = request.Validate();
            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid SetProductFilesRequest: {Request}. Error: {ErrorMsg}", request,
                    validationResult.ErrorMsg);
                return BadRequest();
            }

            var result = await productManagerService.SetProductFiles(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set product files");
            return StatusCode(500);
        }
    }

    [HttpPost("finish")]
    public async Task<ActionResult<bool>> FinishProductCreation([FromBody] int productId)
    {
        try
        {
            var result = await productManagerService.FinishProductCreation(UserId, productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to finish creation of product {ProductId}", productId);
            return StatusCode(500);
        }
    }

    [HttpPost("products")]
    [ExtractLocale]
    public async Task<ActionResult<GetProductListResponse>> GetProductList([FromBody] GetProductListRequest request)
    {
        try
        {
            var result = await productManagerService.GetProductList(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get product list");
            return StatusCode(500);
        }
    }

    [HttpPost("change-status")]
    public async Task<ActionResult<bool>> ChangeProductStatus([FromBody] ChangeProductStatusRequest request)
    {
        try
        {
            var result = await productManagerService.ChangeProductStatus(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to change product status");
            return StatusCode(500);
        }
    }
}
