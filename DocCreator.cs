using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Collections.Generic;
using Ascon.Pilot.SDK.Menu;
using Ascon.Pilot.SDK.ObjectsSample;





namespace Ascon.Pilot.SDK.PilotDocCreator
{
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class DocCreator : IMenu<ObjectsViewContext>
    {
        private readonly IObjectModifier _modifier;
        private readonly IObjectsRepository _repository;
        private const string CREATE_PROJ_DOC = "CreateProjDocMenuItem"; 
        private const string CREATE_PROJ_DOC_LABEL = "Создать документ с исходным файлом";
        private const string COPY_NAME_TO_CLIPBOARD = "CopyNameToClipboard";
        private const string COPY_NAME_TO_CLIPBOARD_LABEL = "Копировать номер и наименование";
        private IDataObject _selected;
        private DataObjectWrapper _selectedDOW;
        private AccessLevel _accessLevel;


        [ImportingConstructor]
        public DocCreator(IObjectModifier modifier, IObjectsRepository repository)
        {
            _modifier = modifier;
            _repository = repository;

        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {

            _selected = context.SelectedObjects.ToList().First();
            _selectedDOW = new DataObjectWrapper(_selected, _repository);
            _accessLevel = GetMyAccessLevel(_selectedDOW); //проверка уровня доступа
            bool notFrozen = !(_selectedDOW.StateInfo.State.ToString().Contains("Frozen")); //проверка, не заморожен ли документ
            var insertIndex = 0; //порядковый номер пункта в меню
            if (context.IsContext && "project_document_folder" == _selected.Type.Name) //если щелчёк произошел по пустому месту и тип объекта - проектный документ
                builder.AddItem(CREATE_PROJ_DOC, insertIndex).WithHeader(CREATE_PROJ_DOC_LABEL)
                                                             .WithIsEnabled((((int)_accessLevel & 16) != 0) & notFrozen); //добавить пункт меню, активный, еслм докумет не замомрожен и есть права на его редактирование
            if ("project_document_ecm" == _selected.Type.Name) //если объект является документом с исходным файлом
                builder.AddItem(COPY_NAME_TO_CLIPBOARD, insertIndex).WithHeader(COPY_NAME_TO_CLIPBOARD_LABEL); //добавить пункт меню "копировать номер и наименование"
        }


        public void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name == CREATE_PROJ_DOC)
            {
                var newDocId = Guid.NewGuid(); //создание нового ID объекта
                _selected.Attributes.TryGetValue("project_document_number", out var parentNumber); //сбор атрибутов с родителя
                _selected.Attributes.TryGetValue("project_document_name", out var parentName); //сбор атрибутов с родителя
                var docType = _repository.GetType("project_document_ecm"); //тип документа для создания нового документа
                _modifier.Create(newDocId, _selected, docType).SetAttribute("project_document_number", parentNumber.ToString())
                                                              .SetAttribute("project_document_name", parentName.ToString())
                                                              .SetAttribute("revision_symbol", "0"); //создание пустого документа с исходным файлом и атрибутами родителя
                _modifier.Apply();
            }

            if (name == COPY_NAME_TO_CLIPBOARD)
            {
                string docRevString = ""; //создание переменной для ревизии
                _selected.Attributes.TryGetValue("project_document_number", out var docNumber); //сбор атрибутов с родителя
                _selected.Attributes.TryGetValue("project_document_name", out var docName); //сбор атрибутов с родителя
                if (_selected.Attributes.TryGetValue("revision_symbol", out var docRev)) //если чтение ревизии прошло успешно, 
                    docRevString = docRev.ToString() + " - ";
                System.Windows.Forms.Clipboard.SetText(docNumber.ToString() + " - " + docRevString + docName.ToString()); //копировать в буфер обмена номер, ревизию и наименование
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
