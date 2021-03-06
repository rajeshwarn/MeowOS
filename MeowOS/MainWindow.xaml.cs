﻿using MeowOS.FileSystem;
using MeowOS.FileSystem.Exceptions;
using MeowOS.ProcScheduler;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemController fsctrl;
        private FileView selection;

        public MainWindow(string path)
        {
            InitializeComponent();
            try
            {
                fsctrl = new FileSystemController();
                fsctrl.openSpace(path);
                openPath(fsctrl.CurrDir);
            }
            catch
            {

            }
            Title = "MeowOS - " + Session.userInfo.Login;

            showHiddenItem.IsEnabled = Session.userInfo.Role == UserInfo.Roles.ADMIN;
        }

        //private enum PathTypes { PT_ABSOLUTE, PT_RELATIVE };
        private void openPath(string path)
        {
            path = UsefulThings.clearExcessSeparators(path);
            try
            {
                path = UsefulThings.clearExcessSeparators(path);
                string tmpPathWithoutLast, tmpLast;
                UsefulThings.detachLastFilename(path, out tmpPathWithoutLast, out tmpLast);
                uint newCluster;
                FileHeader fh;
                if (path.Equals(fsctrl.CurrDir))
                {
                    newCluster = fsctrl.CurrDirCluster;
                    fh = new FileHeader("", "", (byte)FileHeader.FlagsList.FL_DIRECTORY, 0, 0); //требуется лишь только, чтобы у заголовка был флаг "директория"
                }
                else if (tmpPathWithoutLast.Equals(fsctrl.CurrDir))
                {
                    FileHeader tmpFH = fsctrl.getFileHeader(tmpLast, fsctrl.CurrDirCluster, true);
                    if (tmpFH == null)
                        throw new InvalidPathException(path);
                    newCluster = tmpFH.FirstCluster;
                    fh = fsctrl.getFileHeader(tmpLast, fsctrl.CurrDirCluster, true);
                }
                else
                {
                    if (path.Equals(""))
                        newCluster = fsctrl.SuperBlock.RootOffset / fsctrl.SuperBlock.ClusterSize;
                    else
                    {
                        FileHeader tmpFH = fsctrl.getFileHeader(path, true);
                        if (tmpFH == null)
                            throw new InvalidPathException(path);
                        newCluster = tmpFH.FirstCluster;
                    }
                    fh = fsctrl.getFileHeader(path, true);
                }

                if (!path.Equals("") && fh == null)
                    throw new InvalidPathException(path);
                if (path.Equals("") || fh.IsDirectory)
                {
                    byte[] dir = fsctrl.readFile(path, true);
                    wrapPanel.Children.Clear();
                    while (dir.Length > 0)
                    {
                        FileHeader currFH = new FileHeader(dir);
                        addFileView(currFH);
                        dir = dir.Skip(FileHeader.SIZE).ToArray();
                    }
                    fsctrl.CurrDir = path;
                    fsctrl.CurrDirCluster = newCluster;
                    addressEdit.Text = fsctrl.CurrDir.Length > 0 ? fsctrl.CurrDir : "/";
                    selection = null;
                    if (fsctrl.CurrDir.Equals(""))
                        backImg.IsEnabled = false;
                    else
                        backImg.IsEnabled = true;
                }
                else
                {
                    FileViewerWindow fvw = new FileViewerWindow(fh, UsefulThings.ENCODING.GetString(fsctrl.readFile(fh, true)));
                    fvw.Title = fh.NamePlusExtensionWithoutZeros;
                    fvw.ShowDialog();
                    if (fvw.IsChanged && MessageBox.Show("Файл был изменён. Сохранить изменения?", "Подтвердите действие",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        fsctrl.rewriteFile(fsctrl.CurrDir, fh, UsefulThings.ENCODING.GetBytes(fvw.textField.Text), true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                openPath(fsctrl.CurrDir);
            }
        }

        private FileView addFileView(FileHeader fh)
        {
            FileView fv = null;
            if (fh.Name.First() != UsefulThings.DELETED_MARK)
            {
                if (!fh.IsHidden || fh.IsHidden && showHiddenItem.IsChecked == true)
                {
                    fv = new FileView(fh);
                    fv.PreviewMouseDown += onFileViewMouseDown;
                    fv.MouseDoubleClick += onFileViewDoubleClick;
                    wrapPanel.Children.Add(fv);
                }
            }
            return fv;
        }

        public void onFileViewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selection != null)
                selection.panel.Background = Brushes.Transparent;
            FileView senderFV = sender as FileView;
            selection = senderFV;
            senderFV.panel.Background = FileView.selectionBrush;
        }

        private void onFileViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileView senderFV = (sender as FileView);
            openPath(fsctrl.CurrDir + "/" + senderFV.FileHeader.NamePlusExtensionWithoutZeros);
        }

        private void onBackLMBUp(object sender, MouseButtonEventArgs e)
        {
            string newPath, tmp;
            UsefulThings.detachLastFilename(fsctrl.CurrDir, out newPath, out tmp);
            openPath(newPath);
        }

        private void MenuItem_UsersManager_Click(object sender, RoutedEventArgs ea)
        {
            byte[] usersData = fsctrl.readFile("/users.sys", false);
            byte[] groupsData = fsctrl.readFile("/groups.sys", false);
            UsersManagerWindow umw = new UsersManagerWindow(usersData, groupsData);
            umw.ShowDialog();

            try
            {
                if (!umw.UsersData.SequenceEqual(usersData))
                {
                    FileHeader usersHeader = fsctrl.getFileHeader("/users.sys", false);
                    fsctrl.rewriteFile("/", usersHeader, umw.UsersData, false);
                }

                if (!umw.GroupsData.SequenceEqual(groupsData))
                {
                    FileHeader groupsHeader = fsctrl.getFileHeader("/groups.sys", false);
                    fsctrl.rewriteFile("/", groupsHeader, umw.GroupsData, false);
                }

                Title = "MeowOS - " + Session.userInfo.Login;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_fsProperties_Click(object sender, RoutedEventArgs e)
        {
            new FileSystemPropertiesWindow(fsctrl.SuperBlock).ShowDialog();
        }

        private void MenuItem_schedulerClick(object sender, RoutedEventArgs e)
        {
            new SchedulerWindow().ShowDialog();
        }

        private void logout(object sender, RoutedEventArgs e)
        {
            fsctrl.closeSpace();
            Session.clear();
            AuthWindow aw = new AuthWindow();
            Close();
            aw.Show();
        }

        private const string APPLAUSE_CMD = "аплодисменты этому эмулятору";
        private static readonly SoundPlayer player = new SoundPlayer(Properties.Resources.applause);
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (addressEdit.Text.ToLower().Equals(APPLAUSE_CMD))
                    player.Play();
                else
                {
                    openPath(addressEdit.Text);
                    addressEdit.SelectionStart = addressEdit.Text.Length;
                }
            }
        }

        private void MenuItem_Create_Click(object sender, RoutedEventArgs e)
        {
            createCmd((sender as Control) == crDirItem);
        }

        private void createCmd(bool isDirectory)
        {
            FileHeader fh = new FileHeader(Session.userInfo);
            fh.IsDirectory = isDirectory;
            if (isDirectory)
            {
                fh.Name = "newdir";
                fh.Extension = "";
            }

            FilePropertiesWindow fpw = new FilePropertiesWindow(fh);
            if (fpw.ShowDialog().Value)
            {
                try
                {
                    fsctrl.writeFile(fsctrl.CurrDir, fh, null, true);
                    FileView fv = addFileView(fh);
                    onFileViewMouseDown(fv, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fh, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }
        }
        
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            openCmd();
        }

        private void openCmd()
        {
            onFileViewDoubleClick(selection, null);
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить " + (selection.FileHeader.IsDirectory ? "директорию" : "файл")
                + " \"" + fsctrl.CurrDir + "/" + selection.FileHeader.NamePlusExtensionWithoutZeros + "\"?", "Подтвердите действие",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                deleteCmd();
            }
        }

        private void deleteCmd()
        {

            try
            {
                fsctrl.deleteFile(fsctrl.CurrDir, selection.FileHeader, true);
                wrapPanel.Children.Remove(selection);
                selection = null;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            copyCmd();
        }

        private void copyCmd()
        {
            fsctrl.writeToBuffer(selection.FileHeader, fsctrl.readFile(selection.FileHeader, false), null);
        }

        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            cutCmd();
        }

        private void cutCmd()
        {
            fsctrl.writeToBuffer(selection.FileHeader, null, fsctrl.CurrDir);
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            if (fsctrl.BufferFH != null)
                pasteCmd();
            else
                MessageBox.Show("В буфере нет информации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void pasteCmd()
        {
            bool success = false;
            if (fsctrl.BufferRestorePath != null) //вставка вырезанного
            {
                try
                {
                    fsctrl.writeHeader(fsctrl.CurrDir, fsctrl.BufferFH, true);
                    fsctrl.deleteHeader(fsctrl.BufferRestorePath, fsctrl.BufferFH, true);
                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    try
                    {
                        if (!(e is FileAlreadyExistException))
                            fsctrl.deleteHeader(fsctrl.CurrDir, fsctrl.BufferFH, false);
                        fsctrl.writeHeader(fsctrl.BufferRestorePath, fsctrl.BufferFH, false);
                    }
                    catch
                    {
                        //ignore                
                    }
                }
            }
            else //вставка скопированного
            {
                try
                {
                    fsctrl.writeFromBuffer(fsctrl.CurrDir);
                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fsctrl.BufferFH, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }

            if (success)
            {
                FileView fv = addFileView(fsctrl.BufferFH);
                if (fv != null)
                    onFileViewMouseDown(fv, null);
            }

            fsctrl.clearBuffer();
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            propertiesCmd();
        }
        
        private void propertiesCmd()
        {
            int headerOffset = (int)fsctrl.getFileHeaderOffset(selection.FileHeader.NamePlusExtensionWithoutZeros, fsctrl.CurrDirCluster, false);
            FilePropertiesWindow fpw = new FilePropertiesWindow(selection.FileHeader);
            if (fpw.ShowDialog().Value)
            {
                fsctrl.writeBytes(headerOffset, selection.FileHeader.toByteArray());
                selection.refresh();
            }
        }

        private void showHiddenChb_Changed(object sender, RoutedEventArgs e)
        {
            openPath(fsctrl.CurrDir);
        }

        private void MenuItem_Upload_Click(object sender, RoutedEventArgs e)
        {
            uploadCmd();
        }

        private void uploadCmd()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog().Value)
            {
                FileHeader fh = new FileHeader(string.Concat(System.IO.Path.GetFileNameWithoutExtension(ofd.SafeFileName).Where(char.IsLetterOrDigit)),
                    string.Concat(System.IO.Path.GetExtension(ofd.SafeFileName).Where(char.IsLetterOrDigit)), 0,
                    Session.userInfo.Uid, Session.userInfo.Gid);
                try
                {
                    byte[] data = File.ReadAllBytes(ofd.FileName);
                    fsctrl.writeFile(fsctrl.CurrDir, fh, data, true);
                    FileView fv = addFileView(fh);
                    onFileViewMouseDown(fv, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fh, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }
        }

        private void MenuItem_Download_Click(object sender, RoutedEventArgs e)
        {
            downloadCmd();
        }

        private void downloadCmd()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = selection.FileHeader.NamePlusExtensionWithoutZeros;
            if (sfd.ShowDialog().Value)
            {
                File.WriteAllBytes(sfd.FileName, fsctrl.readFile(selection.FileHeader, true));
            }
        }

        private void MenuItem_File_Expand(object sender, RoutedEventArgs e)
        {
            openItem.IsEnabled = deleteItem.IsEnabled = copyItem.IsEnabled = cutItem.IsEnabled = propertiesItem.IsEnabled = selection != null;
            pasteItem.IsEnabled = fsctrl.BufferFH != null;
            downloadItem.IsEnabled = selection != null && !selection.FileHeader.IsDirectory;
        }
    }
}
