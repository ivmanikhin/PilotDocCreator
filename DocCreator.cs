using System;
using System.ComponentModel.Composition;
using System.Linq;
using Ascon.Pilot.SDK.Menu;

namespace Ascon.Pilot.SDK.PilotDocCreator
{
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class DocCreator : IMenu<ObjectsViewContext>
    {
        private readonly IObjectModifier _modifier;
        private readonly IObjectsRepository _repository;
        private const string CREATE_PROJ_DOC = "CreateProjDocMenuItem";
        private IDataObject _selected;

        


        [ImportingConstructor]
        public DocCreator(IPilotDialogService dialogService, IObjectModifier modifier, IObjectsRepository repository /*IObjectCardControlProvider objectCardControlProvider*/)
        {
            _modifier = modifier;
            _repository = repository;
        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {
            _selected = context.SelectedObjects.ToList().First();
            //parentID = _selected.ParentId;
            var itemNames = builder.ItemNames.ToList();
            //const string indexItemName = "miCreate";
            var insertIndex = 0; //itemNames.IndexOf(indexItemName);
            if (context.IsContext && "project_document_folder" == _selected.Type.Name)
                builder.AddItem(CREATE_PROJ_DOC, insertIndex).WithHeader("Создать документ с исходным файлом");
        }


        public /*async*/ void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name == CREATE_PROJ_DOC)
            {
                //var newDocId = new Guid();
                var parent = _selected;
                parent.Attributes.TryGetValue("project_document_number", out var parentNumber);
                parent.Attributes.TryGetValue("project_document_name", out var parentName);
                _modifier.Create(/*newDocId, */parent, GetProjDocECMType()).SetAttribute("project_document_number", parentNumber.ToString())
                                                                           .SetAttribute("project_document_name", parentName.ToString())
                                                                           .SetAttribute("revision_symbol", "0");

                _modifier.Apply();
            }
        }

        private IType GetProjFolderType()
        { 
            return _repository.GetType("project_document_folder");
        }

        private IType GetProjDocECMType()
        {
            return _repository.GetType("project_document_ecm");
        }
    }
}
