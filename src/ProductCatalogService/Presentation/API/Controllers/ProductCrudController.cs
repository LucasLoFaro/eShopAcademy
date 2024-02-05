using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Data.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                return Ok(await _productsRepository.GetByIdAsync("id"));
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
        /*
        // POST api/<ProductController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ProductController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ProductController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
