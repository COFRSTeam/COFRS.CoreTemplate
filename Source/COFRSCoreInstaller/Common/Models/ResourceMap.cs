using System.Collections.Generic;
using System.Linq;

namespace COFRS.Template.Common.Models
{
    public class ResourceMap
    {
        public ResourceModel[] Maps { get; set; }

        /// <summary>
        /// Adds an <see cref="EntityModel"/> to the list. 
        /// </summary>
        /// <param name="model">The <see cref="EntityModel"/> to add.</param>
        public void AddModel(ResourceModel model)
        {
            List<ResourceModel> theList;

            if (Maps == null)
            {
                theList = new List<ResourceModel>();
            }
            else
            {
                theList = Maps.ToList();
            }

            theList.Add(model);
            Maps = theList.ToArray();
        }
    }
}
