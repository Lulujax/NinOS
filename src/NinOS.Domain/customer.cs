using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NinOS.Domain
{
    public class customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_customer { get; set; }

        [Required]
        [MaxLength(50)]
        public string customer_code { get; set; }

        [Required]
        [MaxLength(200)]
        public string business_name { get; set; }

        [MaxLength(50)]
        public string rif { get; set; }

        [MaxLength(100)]
        public string contact_name { get; set; }

        [MaxLength(50)]
        public string phone_number { get; set; }

        public string fiscal_address { get; set; }
        
        public string delivery_address { get; set; }

        [MaxLength(100)]
        public string seller_name { get; set; }

        public customer()
        {
            customer_code = string.Empty;
            business_name = string.Empty;
            rif = string.Empty;
            contact_name = string.Empty;
            phone_number = string.Empty;
            fiscal_address = string.Empty;
            delivery_address = string.Empty;
            seller_name = string.Empty;
        }

        public customer(string code, string business, string doc_rif, string contact, string phone, string fiscal, string delivery, string seller)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException(nameof(code));
            if (string.IsNullOrWhiteSpace(business)) throw new ArgumentNullException(nameof(business));

            customer_code = code;
            business_name = business;
            rif = doc_rif ?? string.Empty;
            contact_name = contact ?? string.Empty;
            phone_number = phone ?? string.Empty;
            fiscal_address = fiscal ?? string.Empty;
            delivery_address = delivery ?? string.Empty;
            seller_name = seller ?? string.Empty;
        }
    }
}