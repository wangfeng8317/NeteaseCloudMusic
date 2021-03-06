﻿using NeteaseCloudMusic.Global.Model;
using NeteaseCloudMusic.Services.NetWork;
using NeteaseCloudMusic.Wpf.Extensions;
using NeteaseCloudMusic.Wpf.View.IndirectView;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NeteaseCloudMusic.Wpf.Properties;

namespace NeteaseCloudMusic.Wpf.ViewModel
{
    public class MusicListViewModel : BindableBase
    {
        private const int LimitPerPage = 30;
        private readonly INetWorkServices _netWorkServices;
        private readonly IRegionManager _navigationService;
        private CancellationTokenSource _OffsetCancellationToken;
        private string _selectedTag;
        private string SelectedTag
        {
            get { return this._selectedTag; }
            set
            {
                if (value == this._selectedTag) return;
                this._selectedTag = value;
                CurrentPlayListPageOffset = 0;
                MorePage = true;
                SelectedPlayList.Clear();
            }
        }
        private bool MorePage { get; set; }
        private int CurrentPlayListPageOffset { get; set; }
        public MusicListViewModel(INetWorkServices netWorkServices, IRegionManager navigationService)
        {
            this._netWorkServices = netWorkServices;
            this._navigationService = navigationService;
            CategoryCommand = new DelegateCommand<string>(CategoryCommandExecute);
            InitCategories();
            NextPageCommand = new DelegateCommand(GetCategoryPlayList);
            PlayListSelectedCommand = new DelegateCommand<Global.Model.PlayList>(PlayListSelectedCommandExecute);
        }

        private void PlayListSelectedCommandExecute(PlayList playListModel)
        {
            if (playListModel != null && playListModel.Id != 0)
            {
                var parmater = new NavigationParameters();
                parmater.Add(IndirectView.IndirectViewModelBase.NavigationIdParmmeterName, playListModel.Id);
                this._navigationService.RequestNavigate(Settings.Default.RegionName, nameof(PlayListDetailView), parmater);
            }

        }

        private void CategoryCommandExecute(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                SelectedTag = "全部"; return;
            }
            SelectedTag = tag;
            GetCategoryPlayList();
        }
        private async void GetCategoryPlayList()
        {
            this._OffsetCancellationToken?.Cancel();
            var newCancel = new CancellationTokenSource();
            this._OffsetCancellationToken = newCancel;
            if (string.IsNullOrEmpty(SelectedTag))
            {
                SelectedTag = "全部";
            }
            try
            {
                if (MorePage)
                {
                    var netWorkDataResult = await this._netWorkServices.GetAsync<KeyValuePair<bool, Global.Model.PlayList[]>>("FindMusic", "GetTopPlayListByTag",
                        new
                        {
                            cat = SelectedTag,
                            hot = true,
                            limit = LimitPerPage,
                            offset = CurrentPlayListPageOffset * LimitPerPage
                        });
                    if (netWorkDataResult.Successed)
                    {
                        var temp = netWorkDataResult.Data;
                        MorePage = temp.Key;
                        SelectedPlayList.AddRange(temp.Value);
                        CurrentPlayListPageOffset++;
                    }
                    else
                    {
                        //todo 显示网络提示信息
                    }
                }

            }
            catch (OperationCanceledException)
            {


            }
            if (this._OffsetCancellationToken == newCancel)
                this._OffsetCancellationToken = null;


        }
        /// <summary>
        /// 初始化
        /// </summary>
        private async void InitCategories()
        {
            var tasks = new[] { this._netWorkServices.GetAsync<PlayListCategory[]>("FindMusic", "GetPlayListCategories"),
                this._netWorkServices.GetAsync<PlayListCategory[]>("FindMusic", "GetHotPlayListCategories")
        };
            await Task.WhenAll(tasks);
            if (tasks.All(x=>x.Result.Successed))
            {
                await Task.WhenAll(AllCategories.AddRangeAsync(tasks[0].Result.Data),HotCategories.AddRangeAsync(tasks[1].Result.Data));
            }
            else
            {
                //todo 网络提示信息
            }
        }


        public ObservableCollection<Global.Model.PlayList> SelectedPlayList
        {
            get;
        } = new ObservableCollection<Global.Model.PlayList>();
        public ObservableCollection<Global.Model.PlayListCategory> AllCategories { get; } = new ObservableCollection<Global.Model.PlayListCategory>();
        public ObservableCollection<Global.Model.PlayListCategory> HotCategories { get; } = new ObservableCollection<Global.Model.PlayListCategory>();
        public ICommand CategoryCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PlayListSelectedCommand { get; }
    }
}
