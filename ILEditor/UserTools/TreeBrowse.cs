﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ILEditor.Forms;
using ILEditor.Classes;
using System.Threading;

namespace ILEditor.UserTools
{
    public partial class TreeBrowse : UserControl
    {
        public TreeBrowse()
        {
            InitializeComponent();
        }

        private ValueWindow window;

        private Boolean AddSPF(string Value)
        {
            Boolean added = false;
            string[] path;
            TreeNode lib, spf;

            Value = Value.ToUpper();
            path = Value.Split('/');

            if (IBMiUtils.IsValueObjectName(path[0]) && IBMiUtils.IsValueObjectName(path[1]))
            {
                if (objectList.Nodes.ContainsKey(path[0]))
                    lib = objectList.Nodes[path[0]];
                else
                {
                    lib = new TreeNode(path[0]);
                    lib.Name = path[0];
                    lib.ImageIndex = 1;
                    lib.SelectedImageIndex = lib.ImageIndex;
                    objectList.Nodes.Add(lib);
                }

                if (!lib.Nodes.ContainsKey(path[0]))
                {
                    spf = new TreeNode(path[1]);
                    spf.Name = path[1];
                    spf.Tag = String.Join("/", Value);
                    spf.ImageIndex = 2;
                    spf.SelectedImageIndex = spf.ImageIndex;
                    spf.Nodes.Add("Loading..");
                    //assign tag here also
                    lib.Nodes.Add(spf);
                    added = true;
                }
            }

            return added;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            List<string> Items = IBMi.CurrentSystem.GetValue("TREE_LIST").Split('|').ToList();
            window = new ValueWindow("Library", "Enter source-physical file \nlocation (LIB/OBJ)", 21);
            window.ShowDialog();
            if (window.Successful)
            {
                if (!Items.Contains(window.Value.ToUpper()))
                {

                    if (AddSPF(window.Value))
                    {
                        Items.Add(window.Value.ToUpper());
                        IBMi.CurrentSystem.SetValue("TREE_LIST", String.Join("|", Items));
                    }
                }
            }
        }
        

        private void objectList_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            TreeNode mbr;
            List<TreeNode> items;
            string[] path;
            
            if (node.Tag is string)
            {
                Thread gothread = new Thread((ThreadStart)delegate
                {
                    path = node.Tag.ToString().Split('/');
                    items = new List<TreeNode>();
                    string[][] members = IBMiUtils.GetMemberList(path[0], path[1]);

                    if (members != null)
                    {

                        foreach (String[] member in members)
                        {
                            mbr = new TreeNode(member[0] + "." + member[1].ToLower() + (member[2] == "" ? "" : " - " + member[2]));
                            mbr.Tag = path[0] + '|' + path[1] + '|' + member[0] + '|' + member[1];
                            mbr.ImageIndex = 3;
                            mbr.SelectedImageIndex = mbr.ImageIndex;
                            items.Add(mbr);
                        }

                        if (members.Length == 0)
                        {
                            items.Add(new TreeNode("No members found."));
                        }
                    }
                    else
                    {
                        items.Add(new TreeNode("No members found."));
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        node.Nodes.Clear();
                        node.Nodes.AddRange(items.ToArray());
                    });
                });

                gothread.Start();
            }
        }

        private void objectList_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag == null) { }
            else
            {
                string tag = e.Node.Tag.ToString();
                if (tag != "")
                {
                    string[] path = tag.Split('|');

                    if (path.Length == 4)
                        Editor.OpenMember(path[0], path[1], path[2], path[3], true);
                }
            }
        }

        private void TreeBrowse_Load(object sender, EventArgs e)
        {
            List<string> Items = IBMi.CurrentSystem.GetValue("TREE_LIST").Split('|').ToList();

            foreach(string Item in Items)
            {
                if (Item == "") continue;
                AddSPF(Item);
            }
        }

        private void objectList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (objectList.SelectedNode != null)
                {
                    if (objectList.SelectedNode.Tag != null)
                    { 
                        string path = objectList.SelectedNode.Tag.ToString();
                        if (path.Contains("/"))
                        {
                            var confirmResult = MessageBox.Show("Are you sure to delete this shortcut?",
                                             "Delete shortcut",
                                             MessageBoxButtons.YesNo);

                            if (confirmResult == DialogResult.Yes)
                            {
                                List<string> Items = IBMi.CurrentSystem.GetValue("TREE_LIST").Split('|').ToList();
                                Items.Remove(path);
                                IBMi.CurrentSystem.SetValue("TREE_LIST", String.Join("|", Items));
                                objectList.Nodes.Remove(objectList.SelectedNode);
                            }
                        }
                    }
                }
            }
        }
    }
}
