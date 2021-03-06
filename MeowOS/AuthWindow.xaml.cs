﻿using MeowOS.FileSystem;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void authorize(string login, string password, bool createNew, ContentControl statusControl)
        {
            statusControl.Visibility = Visibility.Hidden;
            FileSystemSettingsWindow fssw = null;
            if (createNew)
            {
                fssw = new FileSystemSettingsWindow();
                if (!fssw.ShowDialog().Value)
                    return;
            }

            FileDialog dialog = createNew ? (FileDialog)new SaveFileDialog() : (FileDialog)new OpenFileDialog();
            dialog.DefaultExt = "mfs";
            dialog.Filter = "Meow disk (*.mfs)|*.mfs";
            UserInfo userInfo = null;
            if (dialog.ShowDialog() == true)
            {
                FileSystemController fsctrl = new FileSystemController();
                SHA1 sha = SHA1.Create();
                string digest = UsefulThings.ENCODING.GetString(sha.ComputeHash(UsefulThings.ENCODING.GetBytes(password)));
                digest = UsefulThings.replaceControlChars(digest);
                bool success;

                try
                {
                    if (createNew)
                    {
                        //Создать
                        //TODO 15.11: запрашивать параметры создаваемого диска?
                        /*ushort clusterSize = FileSystemController.FACTOR * 4; //Блок = 4 КБ
                        ushort rootSize = (ushort)(clusterSize * 10); //Корневой каталог = 10 блоков
                        uint diskSize = 50 * FileSystemController.FACTOR * FileSystemController.FACTOR; //Раздел = 50 МБ (или 1 МБ для тестов)*/
                        fsctrl.SuperBlock = new SuperBlock(fsctrl, "MeowFS", fssw.ClusterSizeBytes, fssw.RootSizeBytes, fssw.DiskSizeBytes);
                        fsctrl.Fat = new FAT(fsctrl, (int)(fssw.DiskSizeBytes / fssw.ClusterSizeBytes));
                        fsctrl.RootDir = UsefulThings.ENCODING.GetBytes(new String('\0', fssw.RootSizeBytes));
                        fsctrl.createSpace(dialog.FileName, login, digest);
                        userInfo = new UserInfo(1, login, 1, UserInfo.DEFAULT_GROUP, UserInfo.Roles.ADMIN);
                        success = true;
                    }
                    else
                    {
                        //Открыть
                        fsctrl.openSpace(dialog.FileName);
                        byte[] users = fsctrl.readFile("/users.sys", false);
                        string[] usersStr = UsefulThings.fileFromByteArrToStringArr(users);
                        string[] tokens = { "", "", "", "" }; //0 = login, 1 = digest, 2 = gid, 3 = role
                        ushort uid;
                        success = false;
                        for (uid = 1; uid <= usersStr.Length && !success; ++uid)
                        {
                            tokens = usersStr[uid - 1].Split(UsefulThings.USERDATA_SEPARATOR.ToString().ToArray(), StringSplitOptions.None);
                            success = tokens[0].ToLower().Equals(login.ToLower()) && tokens[1].Equals(digest);
                        }
                        if (success)
                        {
                            --uid;
                            byte[] groups = fsctrl.readFile("/groups.sys", false);
                            string[] groupsStr = UsefulThings.fileFromByteArrToStringArr(groups);
                            ushort gid = ushort.Parse(tokens[2]); if (gid > groups.Length) gid = 1;
                            userInfo = new UserInfo(uid, tokens[0], gid, groupsStr[gid - 1], (UserInfo.Roles)Enum.Parse(typeof(UserInfo.Roles), tokens[3]));
                        }
                    }
                }
                catch
                {
                    success = false;
                }
                finally
                {
                    fsctrl.closeSpace();
                }

                if (success)
                {
                    Session.userInfo = userInfo;
                    MainWindow mw = new MainWindow(dialog.FileName);
                    Close();
                    mw.Show();
                }
                else
                {
                    statusControl.Content = "Доступ не разрешён";
                    statusControl.Visibility = Visibility.Visible;
                }
            }
        }

        private void openClick(object sender, RoutedEventArgs e)
        {
            authorize(loginEdit.Text, passEdit.Password, false, statusLabel);
        }

        private void createClick(object sender, RoutedEventArgs e)
        {
            authorize(loginEdit.Text, passEdit.Password, true, statusLabel);
        }

        private void loginEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
            openBtn.IsEnabled = createBtn.IsEnabled = loginEdit.Text.Length > 0;
        }
    }
}
