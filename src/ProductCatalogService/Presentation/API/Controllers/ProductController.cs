using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductsRepository _db;
        public ProductController(IProductsRepository productRepository)
        {
            _db = productRepository;
        }

        [HttpGet]
        async public Task<IActionResult> Get()
        {
            try
            {
                return Ok(await _db.GetAllAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        ex = ex.Message,
                        innerEx = ex.InnerException?.Message
                    });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                return Ok(await _db.GetByIdAsync(id));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new {
                        ex = ex.Message,
                        innerEx = ex.InnerException?.Message
                    });
            }
        }

        [HttpGet("MostExpensive")]
        async public Task<IActionResult> GetMostExpensive()
        {
            try
            {
                return Ok(await _db.GetMostExpensive());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _db.AddAsync(product);
                return StatusCode(StatusCodes.Status201Created, "Product created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while adding product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add product");
            }
        }

        [HttpPut()]
        public async Task<IActionResult> Put([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _db.UpdateAsync(product);

                return StatusCode(StatusCodes.Status201Created, "Product updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while updating product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update product");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Product product = await _db.GetByIdAsync(id);
                if (product == null)
                    return BadRequest("Product " + id + " not found.");

                await _db.DeleteAsync(product);
                return StatusCode(StatusCodes.Status201Created, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while deleting product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete product");
            }
        }
    }
}
