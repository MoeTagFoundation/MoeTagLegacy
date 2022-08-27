using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Lang
{
    internal interface ILanguageProvider
    {
        public string GetLanguageName();

        public string GetLanguageNode(LanguageNodeType node, params string[] args);
    }
}
