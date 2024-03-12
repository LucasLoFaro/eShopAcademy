using Application.Interfaces.Data;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/product/crud")]
    [ApiController]
    public class ProductCrudController : ControllerBase
    {

        private readonly IProductsRepository _productsRepository;
        public ProductCrudController(IProductsRepository productsRepository)
        {
            _productsRepository = productsRepository;
        }
        // GET: api/<ProductController>
        [HttpGet]
        async public Task<IActionResult> Get()
        {
            try
            {
                return Ok(await _productsRepository.GetAllAsync());
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

        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                return Ok(await _productsRepository.GetByIdAsync(id));
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
        
        // POST api/<ProductController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _productsRepository.AddAsync(product);
                
                return StatusCode(StatusCodes.Status201Created, "Product created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while adding product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add product");
            }
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _productsRepository.UpdateAsync(id,product);
                return StatusCode(StatusCodes.Status201Created, "Product updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while updating product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update product");
            }
        }

        // DELETE api/<ProductController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Product product = await _productsRepository.GetByIdAsync(id);
                await _productsRepository.DeleteAsync(product);
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
