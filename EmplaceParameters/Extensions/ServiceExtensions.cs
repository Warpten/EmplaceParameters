using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace EmplaceParameters.Extensions
{
    internal static class ServiceExtensions
    {
        public static T GetService<T>(this SVsServiceProvider provider) where T : class
        {
            return (T) provider.GetService(typeof(T));
        }
    }
}
