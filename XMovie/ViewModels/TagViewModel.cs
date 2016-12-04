using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;

namespace XMovie.ViewModels
{
    public class TagViewModel : ViewModelBase
    {
        #region Category
        private string categoryName;
        public string CategoryName
        {
            get { return this.categoryName; }
            set
            {
                SetProperty(ref categoryName, value, "CategoryName");
            }
        }

        public int TagCategoryId { get; set; }

        #endregion

        #region Tags

        private void UpdateSelectedMovieTags()
        {
            // 選択されている動画に設定されているタグをTagsに設定
            using (var context = new XMovieContext())
            {
                var selectedMovieIdList = selectedMovies?.Select(m => ((MovieItemViewModel)m).MovieId);
                if (selectedMovieIdList == null)
                {
                    Tags = new ObservableCollection<Tag>();
                }
                else
                {
                    var tagIdList = context.TagMaps.Where(tm => selectedMovieIdList.Contains(tm.MovieId)).Select(tm => tm.TagId).ToList();
                    var list = context.Tags.Where(t => t.TagCategoryId == TagCategoryId && tagIdList.Contains(t.TagId)).ToList();
                    Tags = new ObservableCollection<Tag>(list);
                }
            }
        }

        private ObservableCollection<Tag> tags;
        public ObservableCollection<Tag> Tags
        {
            get { return tags; }
            set
            {
                SetProperty(ref tags, value, "Tags");
            }
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
            set { SetProperty(ref addTagText, value, "AddTagText"); }
        }

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                if (addTagCommand == null)
                {
                    addTagCommand = new RelayCommand((param) =>
                    {
                        string newTag = (string)param;
                        using (var context = new XMovieContext())
                        {
                            // 1. 新規タグをマスタに追加
                            bool isExist = context.Tags.Any(t => t.Name.Equals(newTag) && t.TagCategoryId == TagCategoryId);
                            Tag tag;
                            if (!isExist)
                            {
                                tag = new Tag()
                                {
                                    TagCategoryId = TagCategoryId,
                                    Name = newTag
                                };
                                context.Tags.Add(tag);
                                context.SaveChanges();
                            }
                            else
                            {
                                tag = context.Tags.Where(t => t.TagCategoryId == TagCategoryId && t.Name.Equals(newTag))
                                                  .Select(t => t)
                                                  .FirstOrDefault();
                            }
                            // 2. 同じタグが設定されていない動画にタグを追加

                            // TagMapから既に同タグが設定されている動画を抽出
                            var targetMovieIdList = SelectedMovies.Select(m => ((MovieItemViewModel)m).MovieId);
                            var ignores = context.TagMaps
                                                 .Where(tm => tm.TagId == tag.TagId && targetMovieIdList.Contains(tm.MovieId))
                                                 .Select(tm => tm.MovieId)
                                                 .ToList();
                            
                            // ignoresに含まれていない動画にのみタグを設定
                            foreach (MovieItemViewModel movie in selectedMovies)
                            {
                                if (!ignores.Contains(movie.MovieId))
                                {
                                    var tagMap = new TagMap() {
                                        TagId = tag.TagId,
                                        MovieId = movie.MovieId
                                    };
                                    context.TagMaps.Add(tagMap);
                                }
                            }
                            context.SaveChanges();
                        }

                        UpdateSelectedMovieTags();
                        AddTagText = ""; // inputのクリア 
                    });
                }
                return addTagCommand;
            }
        }
        #endregion

        #region SelectedMovies

        private ObservableCollection<object> selectedMovies;
        public ObservableCollection<object> SelectedMovies
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
