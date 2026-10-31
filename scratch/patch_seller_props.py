# 1. seller.cs
with open('src/NinOS.Domain/seller.cs', 'r', encoding='utf-8') as f:
    s = f.read()

s = s.replace('\r\n', '\n')

old_field = '''        private string _customer_code_prefix;'''
new_field = '''        private string _customer_code_prefix;
        private decimal _commission_percentage = 0.10m;'''

old_prop = '''        public bool can_receive_commissions { get; set; } = true;'''
new_prop = '''        public bool can_receive_commissions { get; set; } = true;

        public decimal commission_percentage
        {
            get { return _commission_percentage; }
            set
            {
                if (value < 0) throw new ArgumentException("El porcentaje de comisión no puede ser negativo.");
                _commission_percentage = value;
            }
        }'''

old_ctor = '''        public seller(string full_name, string seller_code, string customer_code_prefix)
        {
            this.full_name = full_name;
            this.seller_code = seller_code;
            this.customer_code_prefix = customer_code_prefix;
            is_active = true;
        }'''

new_ctor = '''        public seller(string full_name, string seller_code, string customer_code_prefix, decimal commission_percentage = 0.10m)
        {
            this.full_name = full_name;
            this.seller_code = seller_code;
            this.customer_code_prefix = customer_code_prefix;
            this.commission_percentage = commission_percentage;
            is_active = true;
            can_receive_commissions = true;
        }'''

assert old_field in s, 'old_field not found'
assert old_prop in s, 'old_prop not found'
assert old_ctor in s, 'old_ctor not found'

s = s.replace(old_field, new_field, 1)
s = s.replace(old_prop, new_prop, 1)
s = s.replace(old_ctor, new_ctor, 1)

with open('src/NinOS.Domain/seller.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(s)
print('seller.cs updated')

# 2. commission_receipt_dto.cs
with open('src/NinOS.Domain/ViewModels/commission_receipt_dto.cs', 'r', encoding='utf-8') as f:
    c = f.read()

c = c.replace('\r\n', '\n')

old_dto = '''        public decimal early_payment_discount { get; set; }
        public decimal commission_10 { get; set; }'''

new_dto = '''        public decimal early_payment_discount { get; set; }
        public decimal commission_percentage { get; set; } = 0.10m;
        public decimal commission_10 { get; set; }'''

assert old_dto in c, 'old_dto not found'
c = c.replace(old_dto, new_dto, 1)

with open('src/NinOS.Domain/ViewModels/commission_receipt_dto.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(c)
print('commission_receipt_dto.cs updated')

# 3. NinOSDbContext.cs
with open('src/NinOS.Infrastructure/data/NinOSDbContext.cs', 'r', encoding='utf-8') as f:
    db = f.read()

db = db.replace('\r\n', '\n')

old_db = '''                entity.Property(e => e.can_receive_commissions).HasColumnName("can_receive_commissions").IsRequired().HasDefaultValue(true);'''
new_db = '''                entity.Property(e => e.can_receive_commissions).HasColumnName("can_receive_commissions").IsRequired().HasDefaultValue(true);
                entity.Ignore(e => e.commission_percentage);'''

assert old_db in db, 'old_db not found'
db = db.replace(old_db, new_db, 1)

with open('src/NinOS.Infrastructure/data/NinOSDbContext.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(db)
print('NinOSDbContext.cs updated')

