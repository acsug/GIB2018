using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using CosmosDb = Microsoft.Azure.Documents;

using GIB2018API.DataAccess;
using GIB2018API.Model;

namespace GIB2018API.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        IDataAccess<Product> _productDbAccess;

        public ProductsController(IDataAccess<Product> customerDbAccess)
        {
            _productDbAccess = customerDbAccess;
        }

        // GET api/products
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Product>), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetMany([FromQuery(Name = "name")]string name = null)
        {
            var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false)";
            var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Product).Name),
                };

            if (name != null) name = name.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                query = $"{query} and CONTAINS(LOWER(c.name), @Name)";

                parameters.Add(new CosmosDb.SqlParameter("@Name", name.ToLower()));
            }

            var response = await _productDbAccess.SearchQueryAsync(query, parameters);

            if (response == null)
                return BadRequest(new Error("The products do not exist or you don't have permission to view them"));

            return Ok(response);
        }

        // GET api/products/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Product), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetOne(string id)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var response = await _productDbAccess.ReadAsync(id);

                if (response == null || (response.Deleted.HasValue && response.Deleted.Value))
                    return BadRequest(new Error("The product does not exist or you don't have permission to view it"));

                return Ok(response);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // POST api/products
        [HttpPost]
        [ProducesResponseType(typeof(Product), 201)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Post([FromBody]Product product)
        {
            try
            {
                var validationResult = Validate(null, product);
                if (validationResult != null)
                    return BadRequest(validationResult);

                Product dbProduct = null;

                if (!string.IsNullOrEmpty(product.Id))
                    dbProduct = await _productDbAccess.ReadAsync(product.Id);

                if (dbProduct != null)
                    return BadRequest(new Error("The product already exists"));

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and LOWER(c.name) = @Name";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Product).Name),
                    new CosmosDb.SqlParameter("@Name", product.Name.ToLower())
                };

                var dbProducts = await _productDbAccess.SearchQueryAsync(query, parameters);

                if (dbProducts != null && dbProducts.Any())
                    return BadRequest(new Error("A product with the same name already exists"));

                return Created("", await _productDbAccess.SaveAsync(product));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // PUT api/products/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Product), 200)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Put(string id, [FromBody]Product product)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var validationResult = Validate(id, product);
                if (validationResult != null)
                    return BadRequest(validationResult);

                var dbProduct = await _productDbAccess.ReadAsync(product.Id);
                if (dbProduct == null || (dbProduct.Deleted.HasValue && dbProduct.Deleted.Value))
                    return BadRequest(new Error("The product does not exist"));

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and c.id <> @Id and LOWER(c.name) = @Name";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Product).Name),
                    new CosmosDb.SqlParameter("@Id", product.Id),
                    new CosmosDb.SqlParameter("@Name", product.Name.ToLower())
                };

                var dbProducts = await _productDbAccess.SearchQueryAsync(query, parameters);

                if (dbProducts != null && dbProducts.Any())
                    return BadRequest(new Error("A product with the same name already exists"));

                return Ok(await _productDbAccess.SaveAsync(product));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // DELETE api/products/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(void), 204)]
        [ProducesResponseType(typeof(Error), 404)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var product = await _productDbAccess.ReadAsync(id);
                if (product == null || (product.Deleted.HasValue && product.Deleted.Value))
                    return BadRequest(new Error("The product does not exist or you don't have permission to delete it"));

                product.Deleted = true;

                product = await _productDbAccess.SaveAsync(product);

                if (product != null)
                    return NoContent();

                return StatusCode(500);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        private Error Validate(string id, Product product)
        {
            if (product == null)
                return new Error("Missing body");

            if (!string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(product.Id))
                    return new Error("Missing value: Product.ID");

                if (!product.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    return new Error("Value missmatch: Product.ID");
            }

            if (string.IsNullOrEmpty(product.Name))
                return new Error("Missing value: Product.Name");

            if (!product.Cost.HasValue)
                return new Error("Missing value: Product.Cost");
            if (product.Cost.Value <= 0)
                return new Error("Bad value: Product.Cost must be greater than zero");

            if (!product.Tax.HasValue)
                return new Error("Missing value: Product.Tax");
            if (product.Tax.Value < 0)
                return new Error("Bad value: Product.Tax must be greater or equal to zero");

            return null;
        }
    }
}
