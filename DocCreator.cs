using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using Ascon.Pilot.SDK.Menu;
using Ascon.Pilot.SDK.CreateObjectSample;
using Ascon.Pilot.SDK.ObjectsSample;


namespace Ascon.Pilot.SDK.PilotDocCreator
{
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class DocCreator : IMenu<ObjectsViewContext>
    {
        private readonly IObjectModifier _modifier;
        private readonly IObjectsRepository _repository;
        private const string CREATE_PROJ_DOC = "CreateProjDocMenuItem";
        private IDataObject _selected;
        private DataObjectWrapper _selectedDOW;
        private AccessLevel _accessLevel;





        [ImportingConstructor]
        public DocCreator(IPilotDialogService dialogService, IObjectModifier modifier, IObjectsRepository repository)
        {
            _modifier = modifier;
            _repository = repository;
        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {

            _selected = context.SelectedObjects.ToList().First();
            _selectedDOW = new DataObjectWrapper(_selected, _repository);
            _accessLevel = GetMyAccessLevel(_selectedDOW);
            bool notFrozen = !(_selectedDOW.StateInfo.State.ToString().Contains("Frozen"));
            var insertIndex = 0;
            if (context.IsContext && "project_document_folder" == _selected.Type.Name)
                builder.AddItem(CREATE_PROJ_DOC, insertIndex).WithHeader("Создать документ с исходным файлом")
                                                             .WithIsEnabled((((int)_accessLevel & 16) != 0) & notFrozen);
        }


        public void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name == CREATE_PROJ_DOC)
            {
                var loader = new ObjectLoader(_repository);
                var newDocId = Guid.NewGuid();
                var parent = _selected;
                parent.Attributes.TryGetValue("project_document_number", out var parentNumber);
                parent.Attributes.TryGetValue("project_document_name", out var parentName);
                _modifier.Create(newDocId, parent, _repository.GetType("project_document_ecm")).SetAttribute("project_document_number", parentNumber.ToString())
                                                                                               .SetAttribute("project_document_name", parentName.ToString())
                                                                                               .SetAttribute("revision_symbol", "0");
                
                _modifier.Apply();
            }
        }


        private AccessLevel GetMyAccessLevel(DataObjectWrapper element)
        {
            var currentAccesLevel = AccessLevel.None;
            var person = _repository.GetCurrentPerson();
            foreach (var position in person.AllOrgUnits())
            {
                currentAccesLevel |= GetAccessLevel(element, position);
            }

            return currentAccesLevel;
        }

        private AccessLevel GetAccessLevel(DataObjectWrapper element, int positonId)
        {
            var currentAccesLevel = AccessLevel.None;
            var orgUnits = _repository.GetOrganisationUnits().ToDictionary(k => k.Id);
            var accesses = GetAccessRecordsForPosition(element, positonId, orgUnits);
            foreach (var source in accesses)
            {
                currentAccesLevel |= source.Access.AccessLevel;
            }
            return currentAccesLevel;
        }

        private IEnumerable<AccessRecordWrapper> GetAccessRecordsForPosition(DataObjectWrapper obj, int positionId, IDictionary<int, IOrganisationUnit> organisationUnits)
        {
            return obj.Access.Where(x => BelongsTo(positionId, x.OrgUnitId, organisationUnits));
        }

        public static bool BelongsTo(int position, int organisationUnit, IDictionary<int, IOrganisationUnit> organisationUnits)
        {
            Stack<int> units = new Stack<int>();
            units.Push(organisationUnit);
            while (units.Any())
            {
                var unitId = units.Pop();
                if (position == unitId)
                    return true;

                IOrganisationUnit unit;
                if (organisationUnits.TryGetValue(unitId, out unit))
                {
                    foreach (var childUnitId in unit.Children)
                    {
                        units.Push(childUnitId);
                    }
                }
            }
            return false;
        }

    }
}
