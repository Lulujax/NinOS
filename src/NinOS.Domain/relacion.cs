using System;
using System.ComponentModel.DataAnnotations;

namespace NinOS.Domain
{
    public class relacion
    {
        private int _id_relacion;
        private int _relation_number;
        private DateTime _week_start;
        private DateTime _week_end;

        public int id_relacion
        {
            get { return _id_relacion; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_relacion = value;
            }
        }

        public int relation_number
        {
            get { return _relation_number; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _relation_number = value;
            }
        }

        public DateTime week_start
        {
            get { return _week_start; }
            set
            {
                if (value == default) throw new ArgumentException();
                _week_start = value.Date;
            }
        }

        public DateTime week_end
        {
            get { return _week_end; }
            set
            {
                if (value == default) throw new ArgumentException();
                _week_end = value.Date;
            }
        }

        /// <summary>
        /// Relacion de cartera heredada: existe para representar un saldo por cobrar
        /// migrating del Excel, sin notas de entrega propias. El saldo vive en
        /// <see cref="saldo"/> en lugar de venir de la suma de sus notas.
        /// Las relaciones huecas se saltan al buscar o renumerar la relacion de una
        /// semana, para que el flujo normal de Pro Venta no se enganche a una.
        /// </summary>
        public bool es_hueca { get; set; } = false;

        /// <summary>
        /// Saldo por cobrar de la relacion hueca. Para las relaciones normales se
        /// mantiene en 0 y el saldo se calcula sumando sus notas.
        /// </summary>
        public decimal saldo { get; set; } = 0m;

        [MaxLength(500)]
        public string? observaciones { get; set; }

        protected relacion()
        {
        }

        public relacion(int relation_number, DateTime week_start, DateTime week_end)
        {
            this.relation_number = relation_number;
            this.week_start = week_start;
            this.week_end = week_end;
        }
    }
}
