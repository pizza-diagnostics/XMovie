using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using XMovie.Message;
using XMovie.Models;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class TagViewModel : BindableBase
    {
        private IDialogService dialogService;

        private IEventAggregator eventAggregator;

        public TagViewModel()
        {
            eventAggregator = App.Container.Resolve<IEventAggregator>();
            dialogService = App.Container.Resolve<IDialogService>();

            Subscribe();
        }

        private void Subscribe()
        {
            eventAggregator.GetEvent<RemoveTagEvent>().Subscribe(tag =>
            {
                var tagId = (tag).TagId;
                var movieIdList = selectedMovies.Select(m => m.MovieId).ToList();

                using (var repo = new RepositoryService())
                {
                    repo.RemoveTagMaps(tagId, movieIdList);
                }

                UpdateSelectedMovieTags();
                UpdateCategoryTags();

            }, ThreadOption.UIThread);

            eventAggregator.GetEvent<AddTagEvent>().Subscribe(evt =>
            {
                // カテゴリIDが同じなら追加
                if (evt.Sender != this && evt.Tag.TagCategoryId == TagCategoryId)
                {
                    if (!Tags.Any(t => t.TagId == evt.Tag.TagId))
                    {
                        var list = new ObservableCollection<Tag>(Tags);
                        list.Add(evt.Tag);
                        Tags = new ObservableCollection<Tag>(list.OrderBy(t => t.Name));
                    }
                    UpdateCategoryTags();
                    AddTagText = ""; // inputのクリア 
                }
            },
            ThreadOption.UIThread);
        }

#region Category
        private string categoryName;
        public string CategoryName
        {
            get { return this.categoryName; }
            set { SetProperty(ref categoryName, value, "CategoryName"); }
        }

        private int tagCategoryId;
        public int TagCategoryId
        {
            get { return tagCategoryId; }
            set
            {
                tagCategoryId = value;
                UpdateCategoryTags();
            }
        }

#endregion

#region Tags

        public void UpdateCategoryTags()
        {
            using (var repo = new RepositoryService())
            {
                var tags = repo.FindCategoryTags(TagCategoryId).Select(t => t.Name);
                CategoryTags = new ObservableCollection<string>(tags);
            }
        }

        private void UpdateSelectedMovieTags()
        {
            // 選択されている動画に設定されているタグをTagsに設定
            var selectedMovieIdList = selectedMovies?.Select(m => m.MovieId);
            if (selectedMovieIdList == null)
            {
                Tags = new ObservableCollection<Tag>();
            }
            else
            {
                using (var repos = new RepositoryService())
                {
                    Tags = new ObservableCollection<Tag>(repos.FindMovieTags(selectedMovieIdList.ToList(), TagCategoryId));
                }
            }
        }

        private ObservableCollection<Tag> tags;
        public ObservableCollection<Tag> Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value, "Tags"); }
        }

        private ObservableCollection<string> categoryTags;
        public ObservableCollection<string> CategoryTags
        {
            get { return categoryTags; }
            set { SetProperty(ref categoryTags, value, "CategoryTags"); }

        }

        private bool enableTagEdit = false;
        public bool EnableTagEdit
        {
            get { return enableTagEdit; }
            set { SetProperty(ref enableTagEdit, value, "EnableTagEdit"); }
        }

        private string addTagText;
        public string AddTagText
        {
            get { return addTagText; }
            set {
                if (SetProperty(ref addTagText, value, "AddTagText"))
                {
                    // TODO
                }
            }
        }

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                return addTagCommand ?? (addTagCommand = new DelegateCommand<TagCommandParameter>((tagParam) =>
                {
                    Tag tag = tagParam.Tag;
                    using (var repos = new RepositoryService())
                    {
                        if (tag == null)
                        {
                            // Tagがnullの場合は新規の可能性
                            if (String.IsNullOrWhiteSpace(tagParam.Name))
                                return;
                            tag = repos.InsertNewTag(tagParam.Name, tagParam.TagCategoryId);
                        }

                        eventAggregator.GetEvent<AddTagEvent>().Publish(new AddTagEventItem()
                        {
                            Tag = tag,
                            Sender = this
                        });

                        // カテゴリIDが同じなら追加
                        /*
                        MovieInformation.AddTagCommand.Execute(tag);
                        foreach (MovieItemViewModel movie in MovieInformation.SelectedMovies)
                        {
                            movie.AddTagCommand.Execute(tag);
                        }
                        */

                        // 表示更新
                        if (!Tags.Any(t => t.TagId == tag.TagId))
                        {
                            var list = new ObservableCollection<Tag>(Tags);
                            list.Add(tag);
                            Tags = new ObservableCollection<Tag>(list.OrderBy(t => t.Name));
                        }
                        UpdateCategoryTags();
                        AddTagText = ""; // inputのクリア 
                    }
                },
                (param) => { return param != null; }
                ));
            }
        }

        private ICommand removeTagCommand;
        public ICommand RemoveTagCommand
        {
            get
            {
                return removeTagCommand ?? (removeTagCommand = new DelegateCommand<Tag>((tag) =>
                {
                    eventAggregator.GetEvent<RemoveTagEvent>().Publish(tag);
                }));
            }
        }

        private ICommand removeCategoryCommand;
        public ICommand RemoveCategoryCommand
        {
            get
            {
                return removeCategoryCommand ?? (removeCategoryCommand = new DelegateCommand<TagViewModel>(async (tag) =>
                {
                    var result = await dialogService.ShowConfirmDialog("カテゴリの削除",
                        "カテゴリを削除しますか?\n(全ての動画からカテゴリに属するすべてのタグが削除されます。)");
                    if (result)
                    {
                        eventAggregator.GetEvent<RemoveCategoryEvent>().Publish(tag);
                    }
                }));
            }
        }

#endregion

#region SelectedMovies

        private ObservableCollection<MovieItemViewModel> selectedMovies;
        public ObservableCollection<MovieItemViewModel> SelectedMovies
        {
            get { return selectedMovies; }
            set
            {
                selectedMovies = value;
                EnableTagEdit = selectedMovies?.Count > 0;
                UpdateSelectedMovieTags();
            }
        }

#endregion
    }
}
