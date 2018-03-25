using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using CosmosDb = Microsoft.Azure.Documents;

using GIB2018API.DataAccess;
using GIB2018API.Model;
using GIB2018API.Serialization;

namespace GIB2018API.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        IDataAccess<Customer> _customerDbAccess;
        IDataAccess<Product> _productDbAccess;
        IDataAccess<Order> _orderDbAccess;

        public OrdersController(IDataAccess<Customer> customerDbAccess, IDataAccess<Product> productDbAccess, IDataAccess<Order> orderDbAccess)
        {
            _customerDbAccess = customerDbAccess;
            _productDbAccess = productDbAccess;
            _orderDbAccess = orderDbAccess;
        }

        // GET api/orders
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Order>), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetMany([FromQuery(Name = "customer_id")]string customerId = null)
        {
            var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false)";
            var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Order).Name),
                };

            if (customerId != null) customerId = customerId.Trim();
            if (!string.IsNullOrEmpty(customerId))
            {
                query = $"{query} and c.customer.id = @CustomerId";

                parameters.Add(new CosmosDb.SqlParameter("@CustomerId", customerId));
            }

            var response = await _orderDbAccess.SearchQueryAsync(query, parameters);

            if (response == null)
                return BadRequest(new Error("The orders do not exist or you don't have permission to view them"));

            return Ok(response);
        }

        // GET api/orders/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Order), 200)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> GetOne(string id)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var response = await _orderDbAccess.ReadAsync(id);

                if (response == null || (response.Deleted.HasValue && response.Deleted.Value))
                    return BadRequest(new Error("The order does not exist or you don't have permission to view it"));

                return Ok(response);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // POST api/orders
        [HttpPost]
        [ProducesResponseType(typeof(Order), 201)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Post([FromBody]Order order)
        {
            try
            {
                var validationResult = Validate(null, order);
                if (validationResult != null)
                    return BadRequest(validationResult);

                Order dbOrder = null;

                if (!string.IsNullOrEmpty(order.Id))
                    dbOrder = await _orderDbAccess.ReadAsync(order.Id);

                if (dbOrder != null)
                    return BadRequest(new Error("The order already exists"));

                order.OrderDate = DateTime.UtcNow;

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and c.customer.id = @CustomerId and c.orderDate.epoch = @OrderDate";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Order).Name),
                    new CosmosDb.SqlParameter("@CustomerId", order.Customer.Id),
                    new CosmosDb.SqlParameter("@OrderDate", order.OrderDate.Value.ToUnixEpoch())
                };

                var dbOrders = await _orderDbAccess.SearchQueryAsync(query, parameters);

                if (dbOrders != null && dbOrders.Any())
                    return BadRequest(new Error("An order for the same customer and timestamp already exists"));

                return Created("", await _orderDbAccess.SaveAsync(order));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // PUT api/orders/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Order), 200)]
        [ProducesResponseType(typeof(Error), 400)]
        [ProducesResponseType(typeof(Error), 500)]
        public async Task<IActionResult> Put(string id, [FromBody]Order order)
        {
            try
            {
                if (id != null) id = id.Trim();
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new Error("Missing parameter: ID"));

                var validationResult = Validate(id, order);
                if (validationResult != null)
                    return BadRequest(validationResult);

                var dbOrder = await _orderDbAccess.ReadAsync(order.Id);
                if (dbOrder == null || (dbOrder.Deleted.HasValue && dbOrder.Deleted.Value))
                    return BadRequest(new Error("The order does not exist"));

                order.OrderDate = dbOrder.OrderDate;

                var query = "SELECT VALUE c FROM c WHERE c['@type'] = @Type and (NOT IS_DEFINED(c.deleted) or c.deleted = false) and c.id <> @Id and c.customer.id = @CustomerId and c.orderDate.epoch = @OrderDate";

                var parameters = new CosmosDb.SqlParameterCollection()
                {
                    new CosmosDb.SqlParameter("@Type", typeof(Order).Name),
                    new CosmosDb.SqlParameter("@Id", order.Id),
                    new CosmosDb.SqlParameter("@CustomerId", order.Customer.Id),
                    new CosmosDb.SqlParameter("@OrderDate", order.OrderDate.Value.ToUnixEpoch())
                };

                var dbOrders = await _orderDbAccess.SearchQueryAsync(query, parameters);

                if (dbOrders != null && dbOrders.Any())
                    return BadRequest(new Error("An order for the same customer and timestamp already exists"));

                return Ok(await _orderDbAccess.SaveAsync(order));
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        // DELETE api/orders/{id}
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

                var order = await _orderDbAccess.ReadAsync(id);
                if (order == null || (order.Deleted.HasValue && order.Deleted.Value))
                    return BadRequest(new Error("The order does not exist or you don't have permission to delete it"));

                order.Deleted = true;

                order = await _orderDbAccess.SaveAsync(order);

                if (order != null)
                    return NoContent();

                return StatusCode(500);
            }
            catch (Exception exception)
            {
                return StatusCode(500, new Error(exception));
            }
        }

        private Error Validate(string id, Order order)
        {
            if (order == null)
                return new Error("Missing body");

            if (!string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(order.Id))
                    return new Error("Missing value: Order.ID");

                if (!order.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    return new Error("Value missmatch: Order.ID");
            }

            if (order.Customer == null || string.IsNullOrEmpty(order.Customer.Id))
                return new Error("Missing value : Order.Customer / Order.Customer.ID");

            var customer = _customerDbAccess.ReadAsync(order.Customer.Id).Result;
            if (customer == null)
                return new Error("Bad value : Order.Customer does not exist or you don't have permission to view it");

            if (order.Items == null || order.Items.Count == 0)
                return new Error("Missing value : Order.Items");

            order.TotalCost = 0;
            order.TotalTax = 0;

            for (var i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];

                if (item.Product == null || string.IsNullOrEmpty(item.Product.Id))
                    return new Error($"Missing value : Order.Items[{i}].Product / Order.Items[{i}].Product.ID");

                if (!item.Quantity.HasValue)
                    return new Error($"Missing value : Order.Items[{i}].Quantity");

                if (item.Quantity.Value < 0)
                    return new Error($"Bad value : Order.Items[{i}].Quantity must be greater than zero");

                var product = _productDbAccess.ReadAsync(item.Product.Id).Result;
                if (product == null)
                    return new Error($"Bad value : Order.Items[{i}].Product does not exist or you don't have permission to view it");

                item.TotalCost = product.Cost.Value * (double)item.Quantity.Value;
                item.TotalTax = product.Tax.Value * (double)item.Quantity.Value;

                order.TotalCost += item.TotalCost;
                order.TotalTax += item.TotalTax;
            }

            return null;
        }
    }
}
