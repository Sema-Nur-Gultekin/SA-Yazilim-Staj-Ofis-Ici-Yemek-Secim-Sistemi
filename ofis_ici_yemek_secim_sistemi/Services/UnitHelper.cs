using System;
using System.Collections.Generic;
using System.Linq;

namespace ofis_ici_yemek_secim_sistemi.Services
{
  
    public static class UnitHelper
    {
        public const string CategoryWeight = "Ağırlık";
        public const string CategoryVolume = "Hacim";
        public const string CategoryCount = "Adet";


        public class UnitDefinition
        {
            public string Value { get; set; }
            public string Label { get; set; }
            public string Category { get; set; }

            public UnitDefinition(string value, string label, string category)
            {
                Value = value;
                Label = label;
                Category = category;
            }
        }

      
        public static readonly List<UnitDefinition> AllUnits = new List<UnitDefinition>
        {
            new UnitDefinition("g",     "Gram (g)",       CategoryWeight),
            new UnitDefinition("kg",    "Kilogram (kg)",  CategoryWeight),
            new UnitDefinition("ml",    "Mililitre (ml)", CategoryVolume),
            new UnitDefinition("lt",    "Litre (lt)",     CategoryVolume),
            new UnitDefinition("adet",  "Adet",           CategoryCount),
            new UnitDefinition("paket", "Paket",          CategoryCount),
            new UnitDefinition("kutu",  "Kutu",           CategoryCount),
        };

        private static string Normalize(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return "";
            string u = unit.Trim().ToLowerInvariant();
            if (u == "g" || u == "gr" || u == "gram" || u == "grams") return "g";
            if (u == "kg" || u == "kilogram" || u == "kilograms") return "kg";
            if (u == "ml" || u == "mililitre" || u == "milliliter" || u == "millilitres") return "ml";
            if (u == "l" || u == "lt" || u == "litre" || u == "liter" || u == "litres") return "lt";
            if (u == "adet" || u == "adt") return "adet";
            if (u == "paket") return "paket";
            if (u == "kutu") return "kutu";
            return u;
        }


        public static string GetCategory(string unit)
        {
            string norm = Normalize(unit);
            var match = AllUnits.FirstOrDefault(u => u.Value == norm);
            return match?.Category;
        }


        public static bool SameCategory(string unitA, string unitB)
        {
            string catA = GetCategory(unitA);
            string catB = GetCategory(unitB);
            return catA != null && catA == catB;
        }


        public static decimal Convert(decimal amount, string fromUnit, string toUnit)
        {
            string from = Normalize(fromUnit);
            string to = Normalize(toUnit);

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                throw new InvalidOperationException("Birim bilgisi eksik; dönüşüm yapılamadı.");

            if (from == to) return amount;

            string catFrom = GetCategory(from);
            string catTo = GetCategory(to);

            if (catFrom == null || catTo == null)
                throw new InvalidOperationException($"'{fromUnit}' veya '{toUnit}' tanınmayan bir birim.");

            if (catFrom != catTo)
                throw new InvalidOperationException($"'{fromUnit}' ({catFrom}) biriminden '{toUnit}' ({catTo}) birimine dönüşüm yapılamaz — farklı ölçü kategorileri birbirine çevrilemez.");

     
            if (from == "kg" && to == "g") return amount * 1000;
            if (from == "g" && to == "kg") return amount / 1000;
            if (from == "lt" && to == "ml") return amount * 1000;
            if (from == "ml" && to == "lt") return amount / 1000;

      
            throw new InvalidOperationException($"'{fromUnit}' biriminden '{toUnit}' birimine otomatik dönüşüm tanımlı değil. Bu tür kalemlerde reçete birimi, stok birimiyle birebir aynı olmalıdır.");
        }
    }
}
