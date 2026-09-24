using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NinOS.UI.Views
{
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _dots_timer;
        private int _dots_count;

        private static readonly string[] _quotes =
        {
            "El éxito es la suma de pequeños esfuerzos repetidos día tras día.",
            "No cuentes los días, haz que los días cuenten.",
            "La disciplina es el puente entre las metas y los logros.",
            "Cree que puedes y ya estás a medio camino.",
            "El único modo de hacer un gran trabajo es amar lo que haces.",
            "La mejor manera de predecir el futuro es creándolo.",
            "Un viaje de mil millas comienza con un solo paso.",
            "Cada día es una nueva oportunidad para cambiar tu vida.",
            "El secreto para avanzar es comenzar.",
            "La motivación te impulsa, el hábito te mantiene.",
            "La distancia entre tus sueños y la realidad se llama acción.",
            "Si puedes soñarlo, puedes lograrlo.",
            "El trabajo duro vence al talento cuando el talento no trabaja duro.",
            "Lo que no te mata te hace más fuerte.",
            "Vive como si fueras a morir mañana; aprende como si fueras a vivir siempre.",
            "No esperes, el momento perfecto nunca llegará.",
            "Donde hay voluntad, hay un camino.",
            "Los grandes logros requieren tiempo y fe en ti mismo.",
            "La persistencia es el camino hacia el éxito.",
            "No eres un fracaso hasta que dejas de intentar.",
            "Hoy es el primer día del resto de tu vida.",
            "La excelencia no es un acto, sino un hábito.",
            "El futuro pertenece a quienes creen en la belleza de sus sueños.",
            "El dolor es temporal, el orgullo es para siempre.",
            "La suerte se fabrica con trabajo duro.",
            "El mejor momento para plantar un árbol fue hace veinte años. El segundo mejor momento es hoy.",
            "No te rindas, el comienzo siempre es la parte más difícil.",
            "La victoria no siempre es ganar, sino nunca darse por vencido.",
            "Haz de cada día tu obra maestra.",
            "El precio del éxito es el trabajo duro, la dedicación y la determinación.",
        };

        public SplashWindow()
        {
            InitializeComponent();
            start_progress_animation();
            show_random_quote();

            _dots_timer = new DispatcherTimer();
            _dots_timer.Interval = TimeSpan.FromMilliseconds(400);
            _dots_timer.Tick += (s, e) =>
            {
                _dots_count = (_dots_count % 3) + 1;
                DotsText.Text = new string('.', _dots_count);
            };
            _dots_timer.Start();
        }

        private readonly Random _random = new Random();

        private void show_random_quote()
        {
            QuoteText.Text = _quotes[_random.Next(_quotes.Length)];
        }

        private void start_progress_animation()
        {
            TranslateTransform translate = new TranslateTransform();
            ProgressRect.RenderTransform = translate;

            DoubleAnimation slide = new DoubleAnimation(-60, 220, TimeSpan.FromMilliseconds(1100));
            slide.RepeatBehavior = RepeatBehavior.Forever;
            translate.BeginAnimation(TranslateTransform.XProperty, slide);
        }

        public void ShowWithAnimation()
        {
            Show();
            Opacity = 1;
        }

        public void CloseWithAnimation()
        {
            _dots_timer.Stop();
            DoubleAnimation fade_out = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fade_out.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fade_out);
        }

        public void SetReady()
        {
            _dots_timer.Stop();
            ProgressRect.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ReadyPanel.Visibility = Visibility.Visible;
            ReadyPanel.Opacity = 0;
            DoubleAnimation fade_in = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            ReadyPanel.BeginAnimation(OpacityProperty, fade_in);
        }
    }
}