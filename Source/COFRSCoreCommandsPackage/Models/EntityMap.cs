using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class EntityMap
    {
        public EntityModel[] Maps { get; set; }

        /// <summary>
        /// Adds an <see cref="EntityModel"/> to the list. 
        /// </summary>
        /// <param name="model">The <see cref="EntityModel"/> to add.</param>
        public void AddModel(EntityModel model)
        {
            List<EntityModel> theList;

            if (Maps == null)
            {
                theList = new List<EntityModel>();
            }
            else
            {
                theList = Maps.ToList<EntityModel>();
            }

            theList.Add(model);
            Maps = theList.ToArray();
        }
    }
}
