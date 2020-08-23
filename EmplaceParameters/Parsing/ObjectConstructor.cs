using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClangSharp;

namespace EmplaceParameters.Parsing
{
    public class Parameter
    {
        public string Name => _parameter.Name;
        public string Type => _parameter.Type.AsString;

        private ParmVarDecl _parameter;

        public Parameter(ParmVarDecl parameter)
        {
            _parameter = parameter;
        }
    }
}
