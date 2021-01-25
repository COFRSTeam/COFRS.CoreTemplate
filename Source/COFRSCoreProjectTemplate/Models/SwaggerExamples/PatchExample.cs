using COFRS;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;

namespace $safeprojectname$.Models.SwaggerExamples
{
    /// <summary>
    /// Example of a patch command list
    /// </summary>
    public class PatchExample : IExamplesProvider<IEnumerable<PatchCommand>>
    {
        private class ExampleAddress
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
        }

        ///	<summary>
        ///	Get Example
        ///	</summary>
        ///	<returns>An example of User</returns>
        public IEnumerable<PatchCommand> GetExamples()
        {
            var patchCommands = new List<PatchCommand>
            {
                new PatchCommand()
                {
                     Op = "REPLACE, ADD, or REMOVE",
                     Path = "the path of the variable to patch",
                     Value = "the new value (ignored on remove)"
                },
                new PatchCommand()
                {
                     Op = "REPLACE",
                     Path = "description",
                     Value = "a description of the item"
                },
                new PatchCommand()
                {
                     Op = "REPLACE",
                     Path = "enabled",
                     Value = true
                },
                new PatchCommand()
                {
                     Op = "REPLACE",
                     Path = "shippingAddress",
                     Value = new ExampleAddress()
                     {
                         Street = "124 E 73rd Ave.",
                         City = "Dallas",
                         State = "TX",
                         PostalCode = "12457",
                         Country = "USA"
                     }
                },
                new PatchCommand()
                {
                     Op = "REPLACE",
                     Path = "shippingAddress/Street",
                     Value = "7200 West Park Blvd."
                }
            };

            return patchCommands.ToArray();
        }
    }
}
