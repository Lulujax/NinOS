with open('src/NinOS.UI/Views/ProVentaHistoryWindow.xaml.cs', 'r', encoding='utf-8') as f:
    c = f.read()

old_block = '''            var vigente = notes.Where(n => !n.esta_anulada).ToList();

            decimal amount = vigente.Sum(n => n.amount);
            decimal paid = payments.Where(p => !p.es_anulacion).Sum(p => p.amount_usd);
            decimal balance = amount - paid;
            if (balance < 0) balance = 0;'''

new_block = '''            var vigente = notes.Where(n => !n.esta_anulada).ToList();

            decimal amount = vigente.Count > 0 ? vigente.Sum(n => n.amount) : _row.amount;
            decimal paid = payments.Where(p => !p.es_anulacion).Sum(p => p.amount_usd);
            if (paid == 0 && _row.paid_amount_usd > 0) paid = _row.paid_amount_usd;
            decimal balance = _row.balance_due_usd;
            if (string.Equals(_row.status, "Pagada", StringComparison.OrdinalIgnoreCase) || balance <= 0.005m)
                balance = 0m;'''

c = c.replace('\r\n', '\n')
old_block = old_block.replace('\r\n', '\n')
new_block = new_block.replace('\r\n', '\n')

assert old_block in c
c = c.replace(old_block, new_block, 1)

with open('src/NinOS.UI/Views/ProVentaHistoryWindow.xaml.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(c)
print('ProVentaHistoryWindow.xaml.cs updated!')

