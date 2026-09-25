using System;

namespace NinOS.Domain
{
    public class sales_goal
    {
        private DateTime _goal_month_start;
        private decimal _amount_usd;

        public int id_sales_goal { get; set; }

        public DateTime goal_month_start
        {
            get { return _goal_month_start; }
            set
            {
                if (value == default) throw new ArgumentException();
                _goal_month_start = value.Date;
            }
        }

        public decimal amount_usd
        {
            get { return _amount_usd; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _amount_usd = value;
            }
        }

        protected sales_goal()
        {
        }

        public sales_goal(DateTime goal_month_start, decimal amount_usd)
        {
            this.goal_month_start = goal_month_start;
            this.amount_usd = amount_usd;
        }
    }
}