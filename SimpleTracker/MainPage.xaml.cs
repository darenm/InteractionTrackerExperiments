using System.Collections.Generic;
using System.Numerics;
using Windows.Devices.Input;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleTracker
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Compositor _compositor;
        private Visual _contentPanelVisual;
        private VisualInteractionSource _interactionSource;
        private Visual _root;
        private InteractionTracker _tracker;
        private ExpressionAnimation m_positionExpression;
        private readonly int pullToRefreshDistance = 5;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void InteractionTrackerSetup(Compositor compositor, Visual hitTestRoot)
        {
            // #1 Create InteractionTracker object
            var tracker = InteractionTracker.Create(compositor);

            // #2 Set Min and Max positions
            tracker.MinPosition = new Vector3(-1000f);
            tracker.MaxPosition = new Vector3(1000f);

            // #3 Setup the VisualInteractionSourc
            var source = VisualInteractionSource.Create(hitTestRoot);

            // #4 Set the properties for the VisualInteractionSource
            source.ManipulationRedirectionMode =
                VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
            source.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
            source.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;

            // #5 Add the VisualInteractionSource to InteractionTracker
            tracker.InteractionSources.Add(source);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ThumbnailList.ItemsSource = new List<Item>
            {
                new Item {Name = "Daren"},
                new Item {Name = "Kendra"},
                new Item {Name = "Gavin"},
                new Item {Name = "Naia"},
                new Item {Name = "Frodo"},
                new Item {Name = "Stella"},
                new Item {Name = "DJ"},
                new Item {Name = "Stasch"}
            };

            // InteractionTracker and VisualInteractionSource setup.
            _root = ElementCompositionPreview.GetElementVisual(Root);
            _contentPanelVisual = ElementCompositionPreview.GetElementVisual(ContentPanel);
            _compositor = _root.Compositor;
            _tracker = InteractionTracker.Create(_compositor);
            _interactionSource = VisualInteractionSource.Create(_root);
            _interactionSource.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;
            _interactionSource.PositionYChainingMode = InteractionChainingMode.Always;
            _tracker.InteractionSources.Add(_interactionSource);
            var refreshPanelHeight = (float) RefreshPanel.ActualHeight;
            _tracker.MaxPosition = new Vector3((float) Root.ActualWidth, 0, 0);
            _tracker.MinPosition = new Vector3(-(float) Root.ActualWidth, -refreshPanelHeight, 0);

            // Use the Tacker's Position (negated) to apply to the Offset of the Image.
            // The -{refreshPanelHeight} is to hide the refresh panel
            m_positionExpression =
                _compositor.CreateExpressionAnimation($"-tracker.Position.Y - {refreshPanelHeight} ");
            m_positionExpression.SetReferenceParameter("tracker", _tracker);
            _contentPanelVisual.StartAnimation("Offset.Y", m_positionExpression);

            var resistanceModifier = CompositionConditionalValue.Create(_compositor);
            var resistanceCondition = _compositor.CreateExpressionAnimation(
                $"-tracker.Position.Y < {pullToRefreshDistance}");
            resistanceCondition.SetReferenceParameter("tracker", _tracker);
            var resistanceAlternateValue = _compositor.CreateExpressionAnimation(
                "source.DeltaPosition.Y / 3");
            resistanceAlternateValue.SetReferenceParameter("source", _interactionSource);
            resistanceModifier.Condition = resistanceCondition;
            resistanceModifier.Value = resistanceAlternateValue;

            var stoppingModifier = CompositionConditionalValue.Create(_compositor);
            var stoppingCondition = _compositor.CreateExpressionAnimation(
                $"-tracker.Position.Y >= {pullToRefreshDistance}");
            stoppingCondition.SetReferenceParameter("tracker", _tracker);
            var stoppingAlternateValue = _compositor.CreateExpressionAnimation("0");
            stoppingModifier.Condition = stoppingCondition;
            stoppingModifier.Value = stoppingAlternateValue;
            //Now add the 2 source modifiers to the InteractionTracker.
            var modifierList = new List<CompositionConditionalValue> {resistanceModifier, stoppingModifier};
            _interactionSource.ConfigureDeltaPositionYModifiers(modifierList);

            //The PointerPressed handler needs to be added using AddHandler method with the //handledEventsToo boolean set to "true"
            //instead of the XAML element's "PointerPressed=Window_PointerPressed",
            //because the list view needs to chain PointerPressed handled events as well.
            ContentPanel.AddHandler(PointerPressedEvent, new PointerEventHandler(Window_PointerPressed), true);
        }

        private void Window_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                // Tell the system to use the gestures from this pointer point (if it can).
                _interactionSource.TryRedirectForManipulation(e.GetCurrentPoint(null));
            }
        }
    }

    public class Item
    {
        public string Name { get; set; }
    }
}