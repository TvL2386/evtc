using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using ScratchEVTCParser.Events;
using ScratchEVTCParser.Model.Agents;

namespace ScratchLogBrowser
{
	public class EventListControl : Panel
	{
		private class FilterType
		{
			public Type Type { get; }
			public bool Checked { get; set; } = true;
			public int Count { get; }

			public FilterType(Type type, int count)
			{
				Type = type;
				Count = count;
			}
		}

		public ICollection<Event> Events
		{
			get => events;
			set
			{
				events.Clear();
				events.AddRange(value);
				eventGridView.DataStore = new FilterCollection<Event>(events) {Filter = GetEventFilter()};

				UpdateFilterView();
			}
		}

		public ICollection<Agent> Agents
		{
			get => agents;
			set
			{
				agents.Clear();
				agents.AddRange(value);
			}
		}

		private readonly List<Event> events = new List<Event>();
		private readonly List<Agent> agents = new List<Agent>();


		private readonly Panel filterPanel = new Panel();
		private readonly GridView<Event> eventGridView;
		private readonly GridView<FilterType> filterGridView;
		private readonly JsonSerializationControl eventJsonControl = new JsonSerializationControl();

		public EventListControl()
		{
			eventGridView = new GridView<Event>();
			eventGridView.Columns.Add(new GridColumn()
			{
				HeaderText = "Time",
				DataCell = new TextBoxCell("Time")
			});
			eventGridView.Columns.Add(new GridColumn()
			{
				HeaderText = "Event Type",
				DataCell = new TextBoxCell()
				{
					Binding = new DelegateBinding<object, string>(x => x.GetType().Name)
				}
			});
			eventGridView.SelectionChanged += (sender, args) => eventJsonControl.Object = eventGridView.SelectedItem;
			eventGridView.Width = 250;

			var dataGroup = new GroupBox
			{
				Padding = new Padding(5), Text = "Event data", Content = eventJsonControl
			};

			var filterLayout = new DynamicLayout();
			var filterGroup = new GroupBox
			{
				Padding = new Padding(5), Text = "Filters", Content = filterLayout
			};

			filterGridView = new GridView<FilterType>();
			filterGridView.Width = 350;
			filterGridView.Height = 100;
			filterGridView.Columns.Add(new GridColumn()
			{
				DataCell = new CheckBoxCell() {Binding = Binding.Property<FilterType, bool?>(x => x.Checked)},
				HeaderText = "Show",
				Editable = true
			});
			filterGridView.Columns.Add(new GridColumn()
			{
				DataCell = new TextBoxCell() {Binding = Binding.Property<FilterType, string>(x => x.Type.Name)},
				HeaderText = "Event Type",
				Editable = false
			});
			filterGridView.Columns.Add(new GridColumn()
			{
				DataCell = new TextBoxCell() {Binding = Binding.Property<FilterType, string>(x => x.Count.ToString())},
				HeaderText = "Event Count",
				Editable = false
			});
			filterGridView.CellEdited += (sender, e) =>
			{
				((FilterCollection<Event>) eventGridView.DataStore).Refresh();
				UpdateFilters();
			};

			var checkAllButton = new Button() {Text = "Check all"};
			checkAllButton.Click += (sender, args) =>
			{
				var dataStore = filterGridView.DataStore?.ToArray();
				if (dataStore == null) return;
				foreach (var filter in dataStore)
				{
					filter.Checked = true;
				}

				filterGridView.DataStore = dataStore;
				((FilterCollection<Event>) eventGridView.DataStore).Refresh();
				UpdateFilters();
			};
			var uncheckAllButton = new Button() {Text = "Uncheck all"};
			uncheckAllButton.Click += (sender, args) =>
			{
				var dataStore = filterGridView.DataStore?.ToArray();
				if (dataStore == null) return;
				foreach (var filter in dataStore)
				{
					filter.Checked = false;
				}

				filterGridView.DataStore = dataStore;
				((FilterCollection<Event>) eventGridView.DataStore).Refresh();
				UpdateFilters();
			};

			filterLayout.BeginHorizontal();
			filterLayout.BeginVertical();
			filterLayout.Add(filterGridView);
			filterLayout.EndVertical();
			filterLayout.BeginVertical(spacing: new Size(5, 5));
			filterLayout.Add(checkAllButton);
			filterLayout.Add(uncheckAllButton);
			filterLayout.Add(null);
			filterLayout.EndVertical();
			filterLayout.Add(null);
			filterLayout.EndHorizontal();
			filterLayout.Add(filterPanel);


			var splitter = new Splitter
				{Panel1 = filterGroup, Panel2 = dataGroup, Orientation = Orientation.Vertical, Position = 200};

			var eventsSplitter = new Splitter {Panel1 = eventGridView, Panel2 = splitter, Position = 300};
			Content = eventsSplitter;
		}

		private void UpdateFilterView()
		{
			var filters = events.GroupBy(x => x.GetType()).Select(x => new FilterType(x.Key, x.Count()))
				.OrderBy(x => x.Type.Name).ToArray();
			filterGridView.DataStore = filters;
		}

		private void UpdateFilters()
		{
			var selectedTypes = filterGridView.DataStore.Where(x => x.Checked).ToArray();
			if (selectedTypes.Length == 0)
			{
				return;
			}

			var commonAncestor = selectedTypes.First().Type;
			foreach (var type in selectedTypes)
			{
				commonAncestor = GetClosestType(commonAncestor, type.Type);
			}

			if (typeof(AgentEvent).IsAssignableFrom(commonAncestor))
			{
				var filterAgentRadioButton = new RadioButton {Text = "Exact Agent"};
				var filterAgentDataRadioButton = new RadioButton(filterAgentRadioButton) {Text = "Agent data"};
				var agentsDropDown = new DropDown
				{
					DataStore = agents,
					ItemTextBinding = new DelegateBinding<Agent, string>(x => $"{x.Name} ({x.Address})")
				};

				var filterLayout = new DynamicLayout();
				filterLayout.BeginHorizontal();
				filterLayout.BeginGroup("Agent filter");
				filterLayout.BeginVertical(spacing: new Size(5, 5));
				filterLayout.AddRow(filterAgentRadioButton, null, agentsDropDown);
				filterLayout.AddRow(filterAgentDataRadioButton, new CheckBox {Text = "Name"}, new TextBox {Text = ""});
				filterLayout.AddRow(null, new CheckBox {Text = "Type"},
					new DropDown {DataStore = new[] {"Player", "NPC", "Gadget"}});
				filterLayout.AddRow(null);
				filterLayout.EndVertical();
				filterLayout.EndGroup();
				filterLayout.EndHorizontal();
				filterPanel.Content = filterLayout;
			}
			else
			{
				filterPanel.Content = null;
			}
		}

		private Func<Event, bool> GetEventFilter()
		{
			return e =>
			{
				var dataStore = filterGridView.DataStore;
				if (dataStore == null) return true;
				return filterGridView.DataStore.FirstOrDefault(x => x.Type == e.GetType())?.Checked ?? true;
			};
		}

		public Type GetClosestType(Type a, Type b)
		{
			while (a != null)
			{
				if (a.IsAssignableFrom(b))
					return a;

				a = a.BaseType;
			}

			return null;
		}
	}
}