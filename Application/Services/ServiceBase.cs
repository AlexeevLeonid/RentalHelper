using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public abstract class ServiceBase
    {
        protected AppDbContext context { get; set; }
        public ServiceBase(AppDbContext context)
        {
            this.context = context;
        }
    }
}
