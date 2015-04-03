using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class CodeItem
    {
        
        public CodeItem() { this.IsEnabled = true; }

        public CodeItem(string id, string name, string desc, string pid)
        {
            this.CodeId = id;
            this.CodeName = name;
            this.Description = desc;
            this.ParentId = pid;
            this.IsEnabled = true;
        }

        public string CodeId { get; set; }
        public string CodeName { get; set; }
        public string Description { get; set; }
        public string ParentId { get; set; }
        public bool IsEnabled { get; set; }
        public IList<string> CodeIndexes { get; set; }

        // used for cache spell
        private string spell = string.Empty;
        public string Spell 
        {
            get { return this.spell; }
        }

        public IDictionary<string, string> Attributes { get; set; }


        public string CalculateSpell()
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();

            string content = this.CodeName;
            if (string.IsNullOrEmpty(this.CodeName))
                dict["|"] = "|";
            else
            {
                dict[content] = content;
                string fullSpell;
                string acronymes = GBKSpell.GetAcronymes(content, out fullSpell);
                dict[fullSpell] = fullSpell;
                dict[acronymes] = acronymes;
            }

            if (false == string.IsNullOrEmpty(this.Description))
            {
                var parts = this.Description.Split(new[] { '|' });
                if (null != this.CodeIndexes)
                {
                    parts = parts.Union(this.CodeIndexes).ToArray();
                }
                foreach (var part in parts)
                {
                    // string fullSpell = GBKSpell.GetFullSpell(part);
                    // string acronymes = GBKSpell.GetAcronymes(part);
                    string fullSpell, acronymes;
                    this.GetSpellCached(part, out fullSpell, out acronymes);
                    dict[fullSpell] = fullSpell;
                    dict[acronymes] = acronymes;
                }
            }
            this.spell = string.Join("|", dict.Values.ToArray());
            return this.spell;
        }

        static Dictionary<char, string> _spellCache = new Dictionary<char, string>();

        public void GetSpellCached(string str, out string spell, out string acronymes)
        {
            spell = acronymes = string.Empty;
            if (string.IsNullOrEmpty(str))
                return;

            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                string sp;
                if (false == _spellCache.TryGetValue(ch, out sp))
                {
                    sp = GBKSpell.ToPinYin(ch.ToString());
                    _spellCache[ch] = sp ?? string.Empty;
                }

                if (false == string.IsNullOrEmpty(sp))
                {
                    spell += sp;
                    acronymes += sp[0];
                }
            }
        }

        public override string ToString() 
        {
            return this.CodeName;
        }
    }
}
