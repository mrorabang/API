using API_Day7.Helpers;
using API_Day7.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace API_Day7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public ProductController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]

        public async Task<IActionResult> GetProducts()
        {
            var pros = await _dbContext.Products.Include(p => p.ProductImages).ToListAsync();
            var res = new ApiResponse(StatusCodes.Status200OK, "Get products successfully", pros);
            return Ok(res);
        }

        [HttpPost]

        public async Task<IActionResult> CreateProducts([FromForm] Product product, List<IFormFile> files)
        {
            object res = null;
            try
            {
                if (ModelState.IsValid)
                {
                    if (files == null)
                    {
                        res = new ApiResponse(StatusCodes.Status400BadRequest, "File is required", null);
                        return BadRequest(res);
                    }
                    await _dbContext.Products.AddAsync(product);
                    foreach (var i in files)
                    {
                        var imgPath = await UploadFile.SaveImage("productImages", i);
                        var img = new ProductImage
                        {
                            Product = product,
                            ImageUrl = imgPath
                        };
                        await _dbContext.ProductImages.AddAsync(img);
                    }
                    await _dbContext.SaveChangesAsync();
                    res = new ApiResponse(StatusCodes.Status201Created, "Create product successfully", product);
                    return Created("success", res);
                }
                res = new ApiResponse(StatusCodes.Status400BadRequest, "Bad Request", null);
                return BadRequest(res);
            }
            catch (Exception ex)
            {
                res = new ApiResponse(StatusCodes.Status500InternalServerError, "Server Error", null);
                return StatusCode(500, res);
            }
        }


        [HttpPut("{id}")]

        public async Task<IActionResult> UpdateProducts(int id, [FromForm] ProductUpdate productUpdate, List<IFormFile>? files)
        {
            object res = null;
            try
            {
                var productExisting = await _dbContext.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == id);
                //ktra co ton tai id san pham ko
                if (productExisting == null)
                {
                    res = new ApiResponse(StatusCodes.Status404NotFound, "Product not found", null);
                    return NotFound(res);
                }

                if (!ModelState.IsValid)
                {
                    res = new ApiResponse(StatusCodes.Status400BadRequest, "Bad request", null);
                    return BadRequest(res);
                }

                productExisting.Name = productUpdate.Name;
                productUpdate.Price = productUpdate.Price;
                productUpdate.Quantity = productUpdate.Quantity;


                List<int> ids = new List<int>();
                if (!string.IsNullOrEmpty(productUpdate.idsToDelete))
                {
                    //chuyen doi chuoi json thanh danh sach kieu int 
                    ids = JsonSerializer.Deserialize<List<int>>(productUpdate.idsToDelete)!;
                }

                //kiem tra neu co ptu nao trong ids 
                if (ids != null && ids.Any())
                {
                    var imagesToDelete = productExisting.ProductImages.Where(img => ids.Contains(img.Id)).ToList();
                    foreach (var i in imagesToDelete)
                    {
                        //xoa file vat ly 
                        if (string.IsNullOrEmpty(i.ImageUrl))
                        {
                            UploadFile.DeleteImage(i.ImageUrl);
                        }
                        _dbContext.ProductImages.Remove(i);

                    }
                }

                if (files != null && files.Any())
                {
                    foreach (var i in files)
                    {
                        var imgPath = await UploadFile.SaveImage("productImages", i);
                        var img = new ProductImage
                        {
                            ProductId = productExisting.Id,
                            ImageUrl = imgPath
                        };
                        await _dbContext.ProductImages.AddAsync(img);
                    }
                }

                //savechange
                await _dbContext.SaveChangesAsync();
                res = new ApiResponse(StatusCodes.Status201Created, "Update product successfully", productExisting);
                return Ok(res);
            }
            catch (Exception ex)
            {
                res = new ApiResponse(StatusCodes.Status500InternalServerError, "Server Error", null);
                return StatusCode(500, res);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            object res = null;
            try
            {
                var productExisting = await _dbContext.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == id);
                if (productExisting == null)
                {
                    res = new ApiResponse(StatusCodes.Status404NotFound, "Product not found", null);
                    return NotFound(res);
                }

                //xoa tat ca file lien quan toi product
                foreach (var i in productExisting.ProductImages)
                {
                    if (!string.IsNullOrEmpty(i.ImageUrl))
                    {
                        UploadFile.DeleteImage(i.ImageUrl);
                    }
                }
                //xoa tat ca ProductImage ra khoi DB
                _dbContext.ProductImages.RemoveRange(productExisting.ProductImages);
                //xoa product ra khoi db
                _dbContext.Products.Remove(productExisting);
                await _dbContext.SaveChangesAsync();
                res = new ApiResponse(StatusCodes.Status200OK, "Delete product successfully", productExisting);
                return Ok(res);

            }
            catch (Exception e)
            {
                res = new ApiResponse(StatusCodes.Status500InternalServerError, "Server errror", null);
                return StatusCode(500,res);
            }
        }

    }
}
