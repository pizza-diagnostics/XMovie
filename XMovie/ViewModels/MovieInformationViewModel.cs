using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Message;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MovieInformationViewModel : BindableBase
    {
        private IEventAggregator eventAggregator = App.Container.Resolve<IEventAggregator>();

        public MovieInformationViewModel()
        {
        }

        public void Initialize()
        {
            Subscribe();

            Tags = new ObservableCollection<TagViewModel>();
            using (var repos = new RepositoryService())
            {
                foreach (var category in repos.GetAllCategories())
                {
                    var tagModel = new TagViewModel()
                    {
                        TagCategoryId = category.TagCategoryId,
                        CategoryName = category.Name
                    };
                    Tags.Add(tagModel);
                }
            }
        }

        private ObservableCollection<MovieItemViewModel> selectedMovies;
        public ObservableCollection<MovieItemViewModel> SelectedMovies
        {
            get { return selectedMovies; }
            set {
                selectedMovies = value;
                if (Tags != null)
                {
                    foreach (var tagViewModel in Tags)
                    {
                        tagViewModel.SelectedMovies = value;
                    }
                }
            }
        }

        private void Subscribe()
        {
            // カテゴリ削除
            eventAggregator.GetEvent<RemoveCategoryEvent>().Subscribe(tagViewModel =>
            {
                var categoryId = tagViewModel.TagCategoryId;

                using (var repos = new RepositoryService())
                {
                    repos.RemoveTagCategory(categoryId);
                }

                Tags.Remove(Tags.Where(t => t.TagCategoryId == categoryId).FirstOrDefault());

            }, ThreadOption.UIThread);

            // タグ削除
            eventAggregator.GetEvent<RemoveTagEvent>().Subscribe(tag =>
            {
                foreach (MovieItemViewModel movie in SelectedMovies)
                {
                    movie.RemoveTag(tag);
                }
            }, ThreadOption.UIThread);

            // タグ追加
            eventAggregator.GetEvent<AddTagEvent>().Subscribe(evt =>
            {
                foreach (MovieItemViewModel movie in SelectedMovies)
                {
                    movie.AddTag(evt.Tag);
                }
            }, ThreadOption.UIThread);
        }

        #region New Category
        private string newCategoryName;
        public string NewCategoryName
        {
            get { return newCategoryName; }
            set
            {
                if (SetProperty(ref newCategoryName, value))
                {
                    OnPropertyChanged("EnableAddCategory");
                }
            }
        }

        public bool EnableAddCategory
        {
            get { return NewCategoryName != null && NewCategoryName.Length > 0; }
        }

        private ICommand addNewCategoryCommand;
        public ICommand AddNewCategoryCommand
        {
            get
            {
                return addNewCategoryCommand ?? (addNewCategoryCommand = new DelegateCommand<string>((categoryName) =>
                {
                    using (var repos = new RepositoryService())
                    {
                        if (!repos.IsExistCategory(categoryName))
                        {
                            var category = repos.InsertNewCategory(categoryName);

                            Tags.Add(new TagViewModel()
                            {
                                TagCategoryId = category.TagCategoryId,
                                CategoryName = category.Name
                            });
                        }
                        NewCategoryName = "";
                    }
                    SelectedMovies = selectedMovies;
                }));
            }
        }

        #endregion

        #region Tag
        private ObservableCollection<TagViewModel> tags;
        public ObservableCollection<TagViewModel> Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value, "Tags"); }
        }

        #endregion
    }
}
