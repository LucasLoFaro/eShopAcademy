using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Domain.Entities;
using Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductService productService;
        private readonly IBlobStorageClient _storageClient;
        public ProductController(IProductService _productService, IBlobStorageClient storageClient)
        {
            productService = _productService;
            _storageClient = storageClient;
        }
        // GET: api/<ProductController>
        [Route("MostExpensive")]
        [HttpGet]
        async public Task<IActionResult> Get()
        {
            try
            {
                return Ok(await productService.GetMostExpensiveProduct());
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        // POST api/<ProductController>
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] ProductWithImage product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await productService.Create(product);

                return StatusCode(StatusCodes.Status201Created, "Product created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while adding product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add product");
            }
        }
    }
}
