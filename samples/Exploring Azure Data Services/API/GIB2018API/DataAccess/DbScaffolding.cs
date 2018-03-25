using System.Net;
using System.Threading.Tasks;

using GIB2018API.Model;

namespace GIB2018API.DataAccess
{
    public class DbScaffolding : IDbScaffolding
    {
        IDataAccess<Product> _productDbAccess;
        IDataAccess<Customer> _customerDbAccess;

        public DbScaffolding(IDataAccess<Product> productDbAccess, IDataAccess<Customer> customerDbAccess)
        {
            _productDbAccess = productDbAccess;
            _customerDbAccess = customerDbAccess;
        }

        public async Task RunAsync()
        {
            var createdResponse = await _productDbAccess.CreateDatabaseAsync();

            if (createdResponse == HttpStatusCode.OK)
                return;

            /*
            for (var i = 1; i < 6;i++)
            {
                var product = new Product() { Name = $"Product {i}", Cost = 10.0 + (double)i, Tax = (10.0 + (double)i) / 10.0 };
                var customer = new Customer() { Name = $"Customer {i}" };

                await _productDbAccess.SaveAsync(product);
                await _customerDbAccess.SaveAsync(customer);
            }
            */
        }
    }
}
