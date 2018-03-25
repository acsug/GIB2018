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
    public class CustomersController : Controller
    {
        IDataAccess<Customer> _customerDbAccess;

        public CustomersController(IDataAccess<Customer> customerDbAccess)
        {
            _customerDbAccess = customerDbAccess;
        }

        // GET api/customers
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Customer>), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetMany([FromQuery(Name = "name")]string name = null)
        {
            var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false)";
            var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Customer).Name),
                };

            if (name != null) name = name.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                query = $"{query} and CONTAINS(LOWER(c.name), @Name)";

                parameters.Add(new CosmosDb.SqlParameter("@Name", name.ToLower()));
            }

            var response = await _customerDbAccess.SearchQueryAsync(query, parameters);

            if (response == null)
                return BadRequest(new Error("The customers do not exist or you don't have permission to view them"));

            return Ok(response);
        }

        // GET api/customers/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Customer), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetOne(string id)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var response = await _customerDbAccess.ReadAsync(id);

                if (response == null || (response.Deleted.HasValue && response.Deleted.Value))
                    return BadRequest(new Error("The customer does not exist or you don't have permission to view it"));

                return Ok(response);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // POST api/customers
        [HttpPost]
        [ProducesResponseType(typeof(Customer), 201)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Post([FromBody]Customer customer)
        {
            try
            {
                var validationResult = Validate(null, customer);
                if (validationResult != null)
                    return BadRequest(validationResult);

                Customer dbCustomer = null;

                if (!string.IsNullOrEmpty(customer.Id))
                    dbCustomer = await _customerDbAccess.ReadAsync(customer.Id);

                if (dbCustomer != null)
                    return BadRequest(new Error("The customer already exists"));

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and LOWER(c.name) = @Name";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Customer).Name),
                    new CosmosDb.SqlParameter("@Name", customer.Name.ToLower())
                };

                var dbCustomers = await _customerDbAccess.SearchQueryAsync(query, parameters);

                if (dbCustomers != null && dbCustomers.Any())
                    return BadRequest(new Error("A customer with the same name already exists"));

                return Created("", await _customerDbAccess.SaveAsync(customer));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // PUT api/customers/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Customer), 200)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Put(string id, [FromBody]Customer customer)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var validationResult = Validate(id, customer);
                if (validationResult != null)
                    return BadRequest(validationResult);

                var dbCustomer = await _customerDbAccess.ReadAsync(customer.Id);
                if (dbCustomer == null || (dbCustomer.Deleted.HasValue && dbCustomer.Deleted.Value))
                    return BadRequest(new Error("The customer does not exist"));

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and c.id <> @Id and LOWER(c.name) = @Name";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Customer).Name),
                    new CosmosDb.SqlParameter("@Id", customer.Id),
                    new CosmosDb.SqlParameter("@Name", customer.Name.ToLower())
                };

                var dbCustomers = await _customerDbAccess.SearchQueryAsync(query, parameters);

                if (dbCustomers != null && dbCustomers.Any())
                    return BadRequest(new Error("A customer with the same name already exists"));

                return Ok(await _customerDbAccess.SaveAsync(customer));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // DELETE api/customers/{id}
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

                var customer = await _customerDbAccess.ReadAsync(id);
                if (customer == null || (customer.Deleted.HasValue && customer.Deleted.Value))
                    return BadRequest(new Error("The customer does not exist or you don't have permission to delete it"));

                customer.Deleted = true;

                customer = await _customerDbAccess.SaveAsync(customer);

                if (customer != null)
                    return NoContent();

                return StatusCode(500);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        private Error Validate(string id, Customer customer)
        {
            if (customer == null)
                return new Error("Missing body");

            if (!string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(customer.Id))
                    return new Error("Missing value: Customer.ID");

                if (!customer.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    return new Error("Value missmatch: Customer.ID");
            }

            if (string.IsNullOrEmpty(customer.Name))
                return new Error("Missing value: Customer.Name");

            return null;
        }
    }
}
