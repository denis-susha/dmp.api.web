using DMP.BL.Models.ProductCreation;
using DMP.BL.Models.Store;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class StoreController(ILogger<StoreController> logger, IStoreService storeService) : ControllerCustom(logger)
{
    private const int MaxStoreImageSize = 2 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<Store>> GetStore()
    {
        try
        {
            var result = await storeService.GetStore(UserId);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get store");
            return StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<ActionResult<bool>> UpdateStore([FromBody] Store store)
    {
        try
        {
            var validationResult = store.Validate();

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await storeService.UpdateStore(UserId, store);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update store");
            return StatusCode(500);
        }
    }

    [RequestSizeLimit(MaxStoreImageSize)]
    [HttpPost("upload-store-image")]
    public async Task<ActionResult<UploadProductImageResponse>> UploadStoreImage([FromForm] IFormCollection formData)
    {
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

            if (memoryStream.Length >= MaxStoreImageSize)
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
            var result = await storeService.UploadStoreImage(UserId, productImgFile);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to upload store image");
            return StatusCode(500);
        }
    }
}
