using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;   // PDF via Microsoft Print to PDF
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace HubBuilders
{
    public partial class MyForm : Form
    {
        // ====== CONSTANT DPI (use readonly to avoid Edit&Continue error) ======
        private static readonly float EXPORT_DPI = 300f;

        private PaperKind lastPaperKind = PaperKind.A4; // print/save ke liye yaad rakhega


        // ===== MenuStrip =====
        private readonly MenuStrip menu = new MenuStrip();
        private readonly ToolStripMenuItem mFile = new ToolStripMenuItem("File");
        private readonly ToolStripMenuItem mEdit = new ToolStripMenuItem("Edit");
        private readonly ToolStripMenuItem mView = new ToolStripMenuItem("View");
        private readonly ToolStripMenuItem mHelp = new ToolStripMenuItem("Help");
        private ToolStripMenuItem miUndo, miRedo;

        // ===== Canvas =====
        private readonly PictureBox pb = new PictureBox() { Dock = DockStyle.Fill, BackColor = Color.White, SizeMode = PictureBoxSizeMode.Zoom };
        private readonly OverlayPanel overlay = new OverlayPanel() { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        private readonly TextBox inline = new TextBox() { Visible = false, BorderStyle = BorderStyle.None };

        // Live big preview while typing
        private readonly Label livePreview = new Label()
        {
            AutoSize = true,
            Visible = false,
            BackColor = Color.FromArgb(235, 255, 255, 210),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(6, 4, 6, 4)
        };
        private bool livePreviewOn = true;

        // Context menu
        private readonly ContextMenuStrip ctx = new ContextMenuStrip();
        private ToolStripMenuItem ctxEdit, ctxDelete, ctxBold, ctxAddHere, ctxAddTickHere, ctxAddTrueHere;
        private ToolStripMenuItem ctxLock, ctxDup, ctxFront, ctxBack, ctxBoxOn, ctxBoxOff, ctxUppercase;
        private ToolStripMenuItem ctxStrike; // NEW
        private ToolStripMenuItem ctxHideImage; // hide template image; text stays for print
        private Point lastContextPoint = Point.Empty;

        // ===== State =====
        private Image template;
        private bool hideTemplateImage = false; // when true: preview/print/save without background image
        private readonly Font baseFont = new Font("Times New Roman", 5f, GraphicsUnit.Point);
        private static readonly float MIN_PT = 1f;
        private float defaultFontPt = 5f;

        // ===== Zoom + Pan =====
        private float zoom = 1.0f;
        private const float ZOOM_MIN = 0.25f, ZOOM_MAX = 4.0f, ZOOM_STEP = 1.1f;
        private PointF pan = new PointF(0, 0);
        private bool spaceHeld = false, panDragging = false;
        private Point panDragStartPt;
        private PointF panAtDragStart;
        private Point lastMouseClient = new Point(0, 0);

        private bool snapToGrid = true;
        private const float SNAP_STEP_IMG = 5f;

        // ===== Tag model =====
        private abstract class TagBase
        {
            public RectangleF ImgRect;
            public bool Selected;
            public bool Locked;
            public abstract void DrawOverlay(Graphics g, RectangleF clientRect, bool showBox);
            public abstract void DrawFinal(Graphics g);
        }

        private sealed class TextTag : TagBase
        {
            public string Text = "YOUR TEXT";
            public bool Bold = true;
            public bool Uppercase = false;
            public bool Strike = false; // NEW
            public float FixedPt = 5f;
            public int BoxCount = 0;

            public Func<TextTag, RectangleF, Tuple<Font, StringFormat>> FontResolver
                = (t, r) => { throw new InvalidOperationException("FontResolver not set"); };

            public override void DrawOverlay(Graphics g, RectangleF rc, bool showBox)
            {
                if (BoxCount > 0) DrawPerBox(g, rc);
                else
                {
                    Tuple<Font, StringFormat> pair = FontResolver(this, rc);
                    string txt = Uppercase ? (Text ?? "").ToUpperInvariant() : (Text ?? "");
                    using (Font f = pair.Item1)
                    using (StringFormat fmt = pair.Item2)
                    {
                        g.DrawString(txt, f, Brushes.Black, rc, fmt);
                    }
                }

                if (showBox)
                {
                    using (var pen = new Pen(Selected ? Color.Red : (Locked ? Color.SlateBlue : Color.Olive), 1f))
                        g.DrawRectangle(pen, rc.X, rc.Y, rc.Width, rc.Height);
                    DrawHandle(g, rc);

                    if (BoxCount > 0)
                    {
                        float cw = rc.Width / Math.Max(1, BoxCount);
                        using (var p = new Pen(Color.Goldenrod, 1f))
                        {
                            p.DashStyle = DashStyle.Dot;
                            for (int i = 1; i < BoxCount; i++)
                                g.DrawLine(p, rc.X + i * cw, rc.Top, rc.X + i * cw, rc.Bottom);
                        }
                    }
                }
            }

            public override void DrawFinal(Graphics g)
            {
                if (BoxCount > 0) { DrawPerBox(g, ImgRect); return; }
                Tuple<Font, StringFormat> pair = FontResolver(this, ImgRect);
                string txt = Uppercase ? (Text ?? "").ToUpperInvariant() : (Text ?? "");
                using (Font f = pair.Item1)
                using (StringFormat fmt = pair.Item2)
                {
                    g.DrawString(txt, f, Brushes.Black, ImgRect, fmt);
                }
            }

            private void DrawPerBox(Graphics g, RectangleF r)
            {
                string src = Uppercase ? (Text ?? "").ToUpperInvariant() : (Text ?? "");
                Tuple<Font, StringFormat> pair = FontResolver(this, r);
                using (Font f = pair.Item1)
                using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    float cw = r.Width / Math.Max(1, BoxCount);
                    for (int i = 0; i < Math.Min(src.Length, BoxCount); i++)
                    {
                        RectangleF cell = new RectangleF(r.X + i * cw, r.Y, cw, r.Height);
                        g.DrawString(src[i].ToString(), f, Brushes.Black, cell, fmt);
                    }
                }
            }
        }

        private sealed class TickTag : TagBase
        {
            public override void DrawOverlay(Graphics g, RectangleF rc, bool showBox)
            {
                DrawTick(g, rc);
                if (showBox)
                {
                    using (var pen = new Pen(Selected ? Color.Red : (Locked ? Color.SlateBlue : Color.DimGray), 1f))
                        g.DrawRectangle(pen, rc.X, rc.Y, rc.Width, rc.Height);
                    DrawHandle(g, rc);
                }
            }
            public override void DrawFinal(Graphics g) { DrawTick(g, ImgRect); }

            private static void DrawTick(Graphics g, RectangleF rc)
            {
                float w = rc.Width, h = rc.Height;
                float stroke = Math.Max(1.5f, Math.Min(w, h) * 0.12f);
                PointF p1 = new PointF(rc.X + 0.15f * w, rc.Y + 0.55f * h);
                PointF p2 = new PointF(rc.X + 0.40f * w, rc.Y + 0.80f * h);
                PointF p3 = new PointF(rc.X + 0.85f * w, rc.Y + 0.25f * h);
                using (var pen = new Pen(Color.Black, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawLines(pen, new[] { p1, p2, p3 });
                }
            }
        }

        private sealed class PhotoTag : TagBase
        {
            public Image Img;
            public bool CoverCrop = false;

            public override void DrawOverlay(Graphics g, RectangleF rc, bool showBox)
            {
                if (Img != null)
                {
                    RectangleF dst = CoverCrop ? FitCover(rc, Img.Width, Img.Height) : FitContain(rc, Img.Width, Img.Height);
                    g.DrawImage(Img, dst);
                }
                if (showBox)
                {
                    using (var p = new Pen(Selected ? Color.Red : (Locked ? Color.SlateBlue : Color.DimGray), 1f))
                        g.DrawRectangle(p, rc.X, rc.Y, rc.Width, rc.Height);
                    DrawHandle(g, rc);
                }
            }
            public override void DrawFinal(Graphics g)
            {
                if (Img != null)
                {
                    RectangleF dst = CoverCrop ? FitCover(ImgRect, Img.Width, Img.Height) : FitContain(ImgRect, Img.Width, Img.Height);
                    g.DrawImage(Img, dst);
                }
            }

            private static RectangleF FitContain(RectangleF rr, int iw, int ih)
            {
                float ir = (float)iw / ih, pr = rr.Width / rr.Height;
                if (ir > pr) { float w = rr.Width, h = w / ir, y = rr.Y + (rr.Height - h) / 2f; return new RectangleF(rr.X, y, w, h); }
                else { float h = rr.Height, w = h * ir, x = rr.X + (rr.Width - w) / 2f; return new RectangleF(x, rr.Y, w, h); }
            }
            private static RectangleF FitCover(RectangleF rr, int iw, int ih)
            {
                float ir = (float)iw / ih, pr = rr.Width / rr.Height;
                if (ir > pr) { float h = rr.Height, w = h * ir, x = rr.X + (rr.Width - w) / 2f; return new RectangleF(x, rr.Y, w, h); }
                else { float w = rr.Width, h = w / ir, y = rr.Y + (rr.Height - h) / 2f; return new RectangleF(rr.X, y, w, h); }
            }
        }

        private readonly List<TagBase> tags = new List<TagBase>();
        private TagBase active;

        private bool dragging, resizing, movedEnough;
        private Point dragStartClient;
        private RectangleF startRectImg;

        private TagBase clipboardTag;

        private string inlineOriginalText = string.Empty;
        private bool ctrlMoveUndoPushed = false;

        private sealed class State { public List<TagBase> Tags; public int ActiveIndex; }
        private readonly Stack<State> undoStack = new Stack<State>();
        private readonly Stack<State> redoStack = new Stack<State>();

        private const int HANDLE = 10, DRAG_THRESHOLD = 3;

        public MyForm()
        {
            Text = "Form Filler (VIP + PDF / C# 7.3)";
            Width = 1200; Height = 800; KeyPreview = true;

            BuildMenu();
            Controls.Add(menu);
            Controls.Add(pb);
            pb.Controls.Add(overlay);
            overlay.Controls.Add(inline);
            overlay.Controls.Add(livePreview);
            inline.Font = new Font(baseFont.FontFamily, defaultFontPt, FontStyle.Regular, GraphicsUnit.Point);
            inline.Visible = false; overlay.BringToFront();

            // ---- Context menu ----
            ctxEdit = new ToolStripMenuItem("Edit Text");
            ctxDelete = new ToolStripMenuItem("Delete");
            ctxBold = new ToolStripMenuItem("Toggle Bold");
            ctxUppercase = new ToolStripMenuItem("Uppercase (On/Off)");
            ctxStrike = new ToolStripMenuItem("Toggle Strikethrough"); // NEW
            ctxAddHere = new ToolStripMenuItem("Add Text Here");
            ctxAddTickHere = new ToolStripMenuItem("Insert ✔ Here");
            ctxAddTrueHere = new ToolStripMenuItem("Insert TRUE Here");
            ctxLock = new ToolStripMenuItem("Lock / Unlock");
            ctxDup = new ToolStripMenuItem("Duplicate");
            ctxFront = new ToolStripMenuItem("Bring to Front");
            ctxBack = new ToolStripMenuItem("Send to Back");
            ctxBoxOn = new ToolStripMenuItem("Per-Box: Set Box Count…");
            ctxBoxOff = new ToolStripMenuItem("Per-Box: Disable");
            ctxHideImage = new ToolStripMenuItem("Hide Image");

            // inline context
            var inlineMenu = new ContextMenuStrip();
            var miInlineFont = new ToolStripMenuItem("Font Size…");
            miInlineFont.Click += (s, e) => ShowFontSizeDialog();
            var miInlineBold = new ToolStripMenuItem("Toggle Bold");
            miInlineBold.Click += (s, e) =>
            {
                if (active is TextTag tt) { PushUndo(); tt.Bold = !tt.Bold; ApplyInlineFont(tt); overlay.Invalidate(); }
            };
            inline.ContextMenuStrip = inlineMenu;
            inlineMenu.Items.AddRange(new ToolStripItem[] { miInlineFont, miInlineBold });
            inline.MouseWheel += Inline_MouseWheel;
            inline.TextChanged += Inline_TextChanged;

            // wire context items
            ctxEdit.Click += (s, e) => ShowInlineForActive();
            ctxDelete.Click += (s, e) => { if (active != null) { PushUndo(); tags.Remove(active); active = null; overlay.Invalidate(); } };
            ctxBold.Click += (s, e) => ToggleBoldActive();
            ctxUppercase.Click += (s, e) => { if (active is TextTag tt) { PushUndo(); tt.Uppercase = !tt.Uppercase; overlay.Invalidate(); if (inline.Visible) ApplyInlineFont(tt); } };
            ctxStrike.Click += (s, e) => { if (active is TextTag tt) { PushUndo(); tt.Strike = !tt.Strike; if (inline.Visible) ApplyInlineFont(tt); overlay.Invalidate(); } }; // NEW
            ctxAddHere.Click += (s, e) => { AddTextTagAndEdit(lastContextPoint); };
            ctxAddTickHere.Click += (s, e) => { lastMouseClient = lastContextPoint; PushUndo(); InsertCheckMarkSmart(); };
            ctxAddTrueHere.Click += (s, e) => { lastMouseClient = lastContextPoint; PushUndo(); InsertTrueSmart(); };
            ctxLock.Click += (s, e) => { if (active != null) { PushUndo(); active.Locked = !active.Locked; overlay.Invalidate(); } };
            ctxDup.Click += (s, e) => { if (active != null) { PushUndo(); clipboardTag = CloneTag(active); PasteClipboardTagAtCursor(); } };
            ctxFront.Click += (s, e) => { if (active != null) { PushUndo(); tags.Remove(active); tags.Add(active); overlay.Invalidate(); } };
            ctxBack.Click += (s, e) => { if (active != null) { PushUndo(); tags.Remove(active); tags.Insert(0, active); overlay.Invalidate(); } };
            ctxBoxOn.Click += (s, e) => SetBoxCountForActive();
            ctxBoxOff.Click += (s, e) => { if (active is TextTag tx) { PushUndo(); tx.BoxCount = 0; overlay.Invalidate(); } };
            ctxHideImage.Click += (s, e) =>
            {
                if (template == null) return;
                hideTemplateImage = !hideTemplateImage;
                ctxHideImage.Text = hideTemplateImage ? "Show Image" : "Hide Image";
                ctxHideImage.Checked = hideTemplateImage;
                overlay.Invalidate();
            };

            ctx.Opening += Ctx_Opening;
            ctx.Items.AddRange(new ToolStripItem[] {
                ctxEdit, ctxBold, ctxUppercase, ctxStrike, ctxBoxOn, ctxBoxOff,
                new ToolStripSeparator(),
                ctxHideImage,
                new ToolStripSeparator(),
                ctxLock, ctxDup, ctxFront, ctxBack, ctxDelete,
                new ToolStripSeparator(),
                ctxAddHere, ctxAddTickHere, ctxAddTrueHere
            });
            overlay.ContextMenuStrip = ctx;

            // overlay events
            overlay.MouseDown += Overlay_MouseDown;
            overlay.MouseMove += Overlay_MouseMove;
            overlay.MouseUp += Overlay_MouseUp;
            overlay.Paint += Overlay_Paint;
            overlay.MouseDoubleClick += Overlay_MouseDoubleClick;

            overlay.MouseEnter += (s, e) => overlay.Focus();
            pb.MouseEnter += (s, e) => overlay.Focus();

            overlay.MouseWheel += Any_MouseWheel;
            pb.MouseWheel += Any_MouseWheel;
            inline.MouseWheel += Any_MouseWheel;
            this.MouseWheel += Any_MouseWheel;

            inline.KeyDown += Inline_KeyDown;
            inline.KeyUp += Inline_KeyUp;
            inline.KeyPress += Inline_KeyPress;
            inline.LostFocus += (s, e) => CommitInline(true);

            var miPrintA4 = new ToolStripMenuItem("Print (A4)") { ShortcutKeys = Keys.Control | Keys.P };
            var miPrintLegal = new ToolStripMenuItem("Print (Legal)");
            miPrintA4.Click += (s, e) => PrintCompositeFlow(PaperKind.A4);
            miPrintLegal.Click += (s, e) => PrintCompositeFlow(PaperKind.Legal);

            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add(miPrintA4);
            mFile.DropDownItems.Add(miPrintLegal);

            // hotkeys
            this.KeyDown += (s, e) =>
            {
                // F2 toggle live preview
                if (e.KeyCode == Keys.F2)
                {
                    livePreviewOn = !livePreviewOn;
                    livePreview.Visible = livePreviewOn && inline.Visible;
                    return;
                }

                if (!inline.Visible && e.KeyCode == Keys.Space) { spaceHeld = true; overlay.Cursor = Cursors.Hand; e.SuppressKeyPress = true; return; }
                if (inline.Visible && e.Control && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y || e.KeyCode == Keys.C || e.KeyCode == Keys.X || e.KeyCode == Keys.V)) return;

                if (e.Control && e.KeyCode == Keys.O) LoadTemplate(null);
                if (e.Control && e.KeyCode == Keys.S) SaveCompositeImage(); // Ctrl+S: PNG + .layout.xml
                if (e.KeyCode == Keys.F12) { SaveCompositeImage(); e.SuppressKeyPress = true; } // F12 quick command

                if (e.Control && !e.Shift && e.KeyCode == Keys.P) { PrintCompositeFlow(PaperKind.A4); e.SuppressKeyPress = true; }
                if (e.Control && e.Shift && e.KeyCode == Keys.P) { SaveCompositeImage(); e.SuppressKeyPress = true; }

                if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)) { AdjustZoom(true, null); e.SuppressKeyPress = true; }
                if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)) { AdjustZoom(false, null); e.SuppressKeyPress = true; }
                if (e.Control && e.KeyCode == Keys.D0) { ResetZoom(); e.SuppressKeyPress = true; }

                if (e.Control && e.KeyCode == Keys.Z) { DoUndo(); e.SuppressKeyPress = true; }
                if (e.Control && e.KeyCode == Keys.Y) { DoRedo(); e.SuppressKeyPress = true; }

                if (e.Control && e.KeyCode == Keys.B) { ToggleBoldActive(); }
                if (e.Control && e.KeyCode == Keys.D) { if (active != null) { PushUndo(); clipboardTag = CloneTag(active); PasteClipboardTagAtCursor(); } }

                if (e.Control && !e.Shift && e.KeyCode == Keys.T) { PushUndo(); InsertCheckMarkSmart(); e.SuppressKeyPress = true; }
                if (e.Control && e.Shift && e.KeyCode == Keys.T) { PushUndo(); InsertTrueSmart(); e.SuppressKeyPress = true; }

                if (!inline.Visible && e.Control && e.KeyCode == Keys.C) { CopyActiveTag(); e.SuppressKeyPress = true; }
                if (!inline.Visible && e.Control && e.KeyCode == Keys.X) { PushUndo(); CutActiveTag(); e.SuppressKeyPress = true; }
                if (!inline.Visible && e.Control && e.KeyCode == Keys.V) { PushUndo(); PasteClipboardTagAtCursor(); e.SuppressKeyPress = true; }

                if (e.Control && e.KeyCode == Keys.L) { if (active != null) { PushUndo(); active.Locked = !active.Locked; overlay.Invalidate(); } }

                if (e.KeyCode == Keys.Delete && active != null) { PushUndo(); tags.Remove(active); active = null; overlay.Invalidate(); }

                if (e.KeyCode == Keys.F1) ShowShortcuts();
                if (e.Control && e.Shift && e.KeyCode == Keys.F) ShowFontSizeDialog();

                if (e.Control && e.KeyCode == Keys.I) InsertPhoto();
                if (e.Control && e.KeyCode == Keys.ShiftKey) { } // ignore
                if (e.Control && e.Shift && e.KeyCode == Keys.I) InsertPassportPhoto();

                if (e.KeyCode == Keys.G) { snapToGrid = !snapToGrid; overlay.Invalidate(); }

                if (e.Control && e.KeyCode == Keys.K) // NEW: Strikethrough
                {
                    if (active is TextTag tt) { PushUndo(); tt.Strike = !tt.Strike; overlay.Invalidate(); if (inline.Visible) ApplyInlineFont(tt); }
                }
            };
            this.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.Space) { spaceHeld = false; panDragging = false; overlay.Cursor = Cursors.IBeam; }
            };

            // drag & drop
            AllowDrop = true;
            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) =>
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0 && File.Exists(files[0]))
                {
                    if (template == null) LoadTemplate(files[0]);
                    else { PushUndo(); AddPhotoTag(files[0]); }
                }
            };

            // auto-load typical names (optional)
            Shown += (s, e) =>
            {
                if (template == null)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] candidates = {
                        Path.Combine(baseDir, "farhadcityform.png"),
                        Path.Combine(baseDir, "FarhadCityForm.png"),
                        Path.Combine(baseDir, "booking_form.png"),
                        Path.Combine(baseDir, "booking_form.jpg")
                    };
                    foreach (string c in candidates) if (File.Exists(c)) { LoadTemplate(c); return; }
                }
            };

            pb.Resize += (s, e) => { overlay.Invalidate(); if (inline.Visible) PositionInlineOnActive(); };

            UpdateUndoRedoMenuEnabled();
        }

        // ===== Photo insert =====
        private void InsertPhoto()
        {
            if (template == null) { MessageBox.Show("Pehle template upload karein.", "Info"); return; }
            using (OpenFileDialog ofd = new OpenFileDialog { Title = "Pick Photo", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                PushUndo();
                AddPhotoTag(ofd.FileName);
            }
        }
        private void InsertPassportPhoto()
        {
            if (template == null) { MessageBox.Show("Pehle template upload karein.", "Info"); return; }
            using (OpenFileDialog ofd = new OpenFileDialog { Title = "Pick Passport Photo (35x45mm)", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                PushUndo();
                AddPassportPhotoTag(ofd.FileName);
            }
        }

        private void ToggleBoldActive()
        {
            if (active is TextTag tt)
            {
                PushUndo();
                tt.Bold = !tt.Bold;
                overlay.Invalidate();
                if (inline.Visible) ApplyInlineFont(tt);
            }
        }

        private void Overlay_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (inline.Visible) CommitInline(true);
            SelectAt(e.Location);
            if (active is TextTag) ShowInlineForActive();
        }

        private void Inline_MouseWheel(object sender, MouseEventArgs e)
        {
            bool shiftOnly = (ModifierKeys & Keys.Shift) == Keys.Shift && (ModifierKeys & Keys.Control) == 0;
            if (!shiftOnly) return;

            if (active is TextTag tt)
            {
                PushUndo();
                float delta = e.Delta > 0 ? 0.5f : -0.5f;
                tt.FixedPt = Math.Max(MIN_PT, tt.FixedPt + delta);
                ApplyInlineFont(tt);
                overlay.Invalidate();
            }
        }

        private void BuildMenu()
        {
            // File
            var miUpload = new ToolStripMenuItem("Upload Template (Ctrl+O)") { ShortcutKeys = Keys.Control | Keys.O };
            var miInsert = new ToolStripMenuItem("Insert Photo (Ctrl+I)") { ShortcutKeys = Keys.Control | Keys.I };
            var miPassPort = new ToolStripMenuItem("Insert Passport Photo 35x45mm (Ctrl+Shift+I)") { ShortcutKeys = Keys.Control | Keys.Shift | Keys.I };
            var miSavePng = new ToolStripMenuItem("Save Image PNG (Ctrl+S)") { ShortcutKeys = Keys.Control | Keys.S };
            var miSavePdf = new ToolStripMenuItem("Save PDF (Ctrl+P)") { ShortcutKeys = Keys.Control | Keys.P };
            var miSaveLayout = new ToolStripMenuItem("Save Layout (.xml)");
            var miLoadLayout = new ToolStripMenuItem("Load Layout (.xml)");
            var miExit = new ToolStripMenuItem("Exit");

            miUpload.Click += (s, e) => LoadTemplate(null);
            miInsert.Click += (s, e) => InsertPhoto();
            miPassPort.Click += (s, e) => InsertPassportPhoto();
            miSavePng.Click += (s, e) => SaveCompositeImage();

            // ---- Hide/disable PDF menu & remove its shortcut ----
            miSavePdf.Click += (s, e) => SaveCompositePdf();
            miSavePdf.ShortcutKeys = Keys.None;
            miSavePdf.Visible = false;
            miSavePdf.Enabled = false;
            // ------------------------------------------------------

            miSaveLayout.Click += (s, e) => SaveLayoutXml();
            miLoadLayout.Click += (s, e) => LoadLayoutXml();
            miExit.Click += (s, e) => Close();

            mFile.DropDownItems.AddRange(new ToolStripItem[] { miUpload, miInsert, miPassPort,
                new ToolStripSeparator(), miSavePng, miSavePdf, miSaveLayout, miLoadLayout,
                new ToolStripSeparator(), miExit });

            // Edit
            miUndo = new ToolStripMenuItem("Undo (Ctrl+Z)") { ShortcutKeys = Keys.Control | Keys.Z };
            miRedo = new ToolStripMenuItem("Redo (Ctrl+Y)") { ShortcutKeys = Keys.Control | Keys.Y };
            miUndo.Click += (s, e) => DoUndo();
            miRedo.Click += (s, e) => DoRedo();

            var miFontSize = new ToolStripMenuItem("Font Size… (Ctrl+Shift+F)") { ShortcutKeys = Keys.Control | Keys.Shift | Keys.F };
            var miBold = new ToolStripMenuItem("Toggle Bold (Ctrl+B)") { ShortcutKeys = Keys.Control | Keys.B };
            var miStrike = new ToolStripMenuItem("Strikethrough (Ctrl+K)") { ShortcutKeys = Keys.Control | Keys.K }; // NEW
            var miDelete = new ToolStripMenuItem("Delete Selected (Del)") { ShortcutKeys = Keys.Delete };
            var miClear = new ToolStripMenuItem("Clear All");
            var miTick = new ToolStripMenuItem("Insert ✔ (Ctrl+T)") { ShortcutKeys = Keys.Control | Keys.T };
            var miTrue = new ToolStripMenuItem("Insert TRUE (Ctrl+Shift+T)") { ShortcutKeys = Keys.Control | Keys.Shift | Keys.T };
            var miCopy = new ToolStripMenuItem("Copy Tag (Ctrl+C)") { ShortcutKeys = Keys.Control | Keys.C };
            var miCut = new ToolStripMenuItem("Cut Tag (Ctrl+X)") { ShortcutKeys = Keys.Control | Keys.X };
            var miPaste = new ToolStripMenuItem("Paste Tag (Ctrl+V)") { ShortcutKeys = Keys.Control | Keys.V };
            var miDuplicate = new ToolStripMenuItem("Duplicate (Ctrl+D)") { ShortcutKeys = Keys.Control | Keys.D };
            var miLock = new ToolStripMenuItem("Lock / Unlock (Ctrl+L)") { ShortcutKeys = Keys.Control | Keys.L };

            miFontSize.Click += (s, e) => ShowFontSizeDialog();
            miBold.Click += (s, e) => ToggleBoldActive();
            miStrike.Click += (s, e) => { if (active is TextTag tt) { PushUndo(); tt.Strike = !tt.Strike; if (inline.Visible) ApplyInlineFont(tt); overlay.Invalidate(); } }; // NEW
            miDelete.Click += (s, e) => { if (active != null) { PushUndo(); tags.Remove(active); active = null; overlay.Invalidate(); } };
            miClear.Click += (s, e) => { PushUndo(); tags.Clear(); active = null; inline.Visible = false; overlay.Invalidate(); };
            miTick.Click += (s, e) => { PushUndo(); InsertCheckMarkSmart(); };
            miTrue.Click += (s, e) => { PushUndo(); InsertTrueSmart(); };
            miCopy.Click += (s, e) => { if (!inline.Visible) CopyActiveTag(); };
            miCut.Click += (s, e) => { if (!inline.Visible) { PushUndo(); CutActiveTag(); } };
            miPaste.Click += (s, e) => { if (!inline.Visible) { PushUndo(); PasteClipboardTagAtCursor(); } };
            miDuplicate.Click += (s, e) => { if (active != null) { PushUndo(); clipboardTag = CloneTag(active); PasteClipboardTagAtCursor(); } };
            miLock.Click += (s, e) => { if (active != null) { PushUndo(); active.Locked = !active.Locked; overlay.Invalidate(); } };

            mEdit.DropDownItems.AddRange(new ToolStripItem[] { miUndo, miRedo, new ToolStripSeparator(),
                miFontSize, miBold, miStrike, miDelete, miClear, new ToolStripSeparator(),
                miTick, miTrue, new ToolStripSeparator(),
                miCopy, miCut, miPaste, miDuplicate, miLock });

            // View
            var miZoomIn = new ToolStripMenuItem("Zoom In (Ctrl+ +)") { ShortcutKeys = Keys.Control | Keys.Oemplus };
            var miZoomOut = new ToolStripMenuItem("Zoom Out (Ctrl+ -)") { ShortcutKeys = Keys.Control | Keys.OemMinus };
            var miZoomReset = new ToolStripMenuItem("Reset Zoom (Ctrl+0)") { ShortcutKeys = Keys.Control | Keys.D0 };
            var miSnap = new ToolStripMenuItem("Snap to 5px Grid (G)") { Checked = snapToGrid };
            miSnap.Click += (s, e) => { snapToGrid = !snapToGrid; miSnap.Checked = snapToGrid; overlay.Invalidate(); };
            var miZoomTip = new ToolStripMenuItem("Tip: Ctrl + MouseWheel se zoom") { Enabled = false };

            miZoomIn.Click += (s, e) => AdjustZoom(true, null);
            miZoomOut.Click += (s, e) => AdjustZoom(false, null);
            miZoomReset.Click += (s, e) => ResetZoom();

            mView.DropDownItems.AddRange(new ToolStripItem[] { miZoomIn, miZoomOut, miZoomReset, new ToolStripSeparator(), miSnap, miZoomTip });

            // Help
            var miShort = new ToolStripMenuItem("Shortcuts (F1)") { ShortcutKeys = Keys.F1 };
            miShort.Click += (s, e) => ShowShortcuts();
            mHelp.DropDownItems.Add(miShort);

            menu.Items.AddRange(new ToolStripItem[] { mFile, mEdit, mView, mHelp });
            MainMenuStrip = menu;
        }

        // ===== Template =====
        private void LoadTemplate(string path)
        {
            if (path == null)
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Title = "Pick Form Image", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" })
                {
                    if (ofd.ShowDialog(this) != DialogResult.OK) return;
                    path = ofd.FileName;
                }
            }
            if (template != null) template.Dispose();
            template = Image.FromFile(path);

            hideTemplateImage = false;
            if (ctxHideImage != null)
            {
                ctxHideImage.Text = "Hide Image";
                ctxHideImage.Checked = false;
            }

            tags.Clear(); active = null; inline.Visible = false;
            zoom = 1.0f; pan = new PointF(0, 0);
            undoStack.Clear(); redoStack.Clear(); UpdateUndoRedoMenuEnabled();
            overlay.Invalidate();

            // NEW: auto-detect sibling .layout.xml and prompt
            try
            {
                string xml = Path.ChangeExtension(path, ".layout.xml");
                if (File.Exists(xml))
                {
                    var r = MessageBox.Show("Is template ke sath layout (.layout.xml) mil gaya. Load karun?",
                                            "Load Layout?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r == DialogResult.Yes) LoadLayoutXmlFrom(xml);
                }
            }
            catch { /* ignore */ }
        }

        // ===== Add Tags =====
        private void AddTextTagAndEdit(Point clickPtClient)
        {
            PushUndo();

            var t = new TextTag();
            t.FixedPt = defaultFontPt;
            t.FontResolver = ResolveFont;

            PointF pImg = ClientToImage(clickPtClient);
            if (!float.IsNaN(pImg.X)) t.ImgRect = new RectangleF(pImg.X - 110, pImg.Y - 9, 220, 18);
            else t.ImgRect = new RectangleF(30, 30, 220, 18);

            FitRectToFont(t);
            if (snapToGrid) SnapRect(ref t.ImgRect);
            ClampToImage(ref t.ImgRect);

            tags.Add(t); active = t; overlay.Invalidate();
            ShowInlineForActive();
        }

        private void AddSpecificTextTagAtPoint(string text, Point clientPt, bool bold = true)
        {
            PushUndo();

            PointF pImg = ClientToImage(clientPt);
            if (float.IsNaN(pImg.X))
            {
                RectangleF vp = GetViewport();
                clientPt = new Point((int)(vp.X + vp.Width / 2f), (int)(vp.Y + vp.Height / 2f));
                pImg = ClientToImage(clientPt);
            }

            var t = new TextTag { Text = text, Bold = bold, FixedPt = defaultFontPt, FontResolver = ResolveFont };
            t.ImgRect = new RectangleF(pImg.X - 18, pImg.Y - 10, 36, 20);

            FitRectToFont(t);
            if (snapToGrid) SnapRect(ref t.ImgRect);
            ClampToImage(ref t.ImgRect);

            tags.Add(t); active = t; overlay.Invalidate();
            ShowInlineForActive();
        }

        private void InsertCheckMarkSmart()
        {
            if (inline.Visible && active is TextTag)
            {
                int selStart = inline.SelectionStart;
                string ins = "✔";
                inline.Text = inline.Text.Insert(selStart, ins);
                inline.SelectionStart = selStart + ins.Length;
                inline.SelectionLength = 0;
                return;
            }

            PointF pImg = ClientToImage(lastMouseClient);
            if (float.IsNaN(pImg.X))
            {
                RectangleF vp = GetViewport();
                lastMouseClient = new Point((int)(vp.X + vp.Width / 2f), (int)(vp.Y + vp.Height / 2f));
                pImg = ClientToImage(lastMouseClient);
            }

            var tick = new TickTag();
            float w = 20, h = 20;
            tick.ImgRect = new RectangleF(pImg.X - w / 2f, pImg.Y - h / 2f, w, h);
            if (snapToGrid) SnapRect(ref tick.ImgRect);
            ClampToImage(ref tick.ImgRect);
            tags.Add(tick); active = tick; overlay.Invalidate();
        }

        private void InsertTrueSmart()
        {
            if (inline.Visible && active is TextTag)
            {
                int selStart = inline.SelectionStart;
                string ins = "TRUE";
                inline.Text = inline.Text.Insert(selStart, ins);
                inline.SelectionStart = selStart + ins.Length;
                inline.SelectionLength = 0;
                return;
            }
            AddSpecificTextTagAtPoint("TRUE", lastMouseClient, true);
        }

        private void AddPhotoTag(string path)
        {
            if (template == null || !File.Exists(path)) return;

            var p = new PhotoTag { Img = Image.FromFile(path) };
            float w = 160, h = 200;
            RectangleF vp = GetViewport();
            PointF pImgTopRight = ClientToImage(new Point((int)(vp.Right - 30), (int)(vp.Top + 30)));
            if (!float.IsNaN(pImgTopRight.X))
                p.ImgRect = new RectangleF(pImgTopRight.X - w, pImgTopRight.Y, w, h);
            else
                p.ImgRect = new RectangleF(Math.Max(0, template.Width - w - 30), 30, w, h);

            if (snapToGrid) SnapRect(ref p.ImgRect);
            ClampToImage(ref p.ImgRect);
            tags.Add(p); active = p; overlay.Invalidate();
        }

        private void AddPassportPhotoTag(string path)
        {
            if (template == null || !File.Exists(path)) return;

            var p = new PhotoTag { Img = Image.FromFile(path) };
            float w = 413f, h = 531f; // ~35x45mm @ 300dpi
            RectangleF vp = GetViewport();
            PointF pImgTopRight = ClientToImage(new Point((int)(vp.Right - 30), (int)(vp.Top + 30)));
            if (!float.IsNaN(pImgTopRight.X))
                p.ImgRect = new RectangleF(pImgTopRight.X - w, pImgTopRight.Y, w, h);
            else
                p.ImgRect = new RectangleF(Math.Max(0, template.Width - w - 30), 30, w, h); // FIX: Math.Max

            if (snapToGrid) SnapRect(ref p.ImgRect);
            ClampToImage(ref p.ImgRect);
            tags.Add(p); active = p; overlay.Invalidate();
        }

        // ===== Context menu visibility logic =====
        private void Ctx_Opening(object sender, CancelEventArgs e)
        {
            bool overTag = false;
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                RectangleF rc = ImageRectToClient(tags[i].ImgRect);
                if (rc.Contains(lastContextPoint)) { overTag = true; active = tags[i]; break; }
            }
            foreach (TagBase t in tags) t.Selected = (t == active);
            overlay.Invalidate();

            if (ctxHideImage != null)
            {
                ctxHideImage.Enabled = (template != null);
                ctxHideImage.Visible = true;
                ctxHideImage.Checked = hideTemplateImage;
                ctxHideImage.Text = hideTemplateImage ? "Show Image" : "Hide Image";
            }

            if (overTag)
            {
                bool isText = active is TextTag;
                ctxEdit.Visible = isText;
                ctxBold.Visible = isText;
                ctxUppercase.Visible = isText;
                ctxStrike.Visible = isText; // NEW
                ctxBoxOn.Visible = isText;
                ctxBoxOff.Visible = isText && ((TextTag)active).BoxCount > 0;

                ctxDelete.Visible = true;
                ctxLock.Visible = true;
                ctxDup.Visible = true;
                ctxFront.Visible = true;
                ctxBack.Visible = true;

                ctxAddHere.Visible = false;
                ctxAddTickHere.Visible = false;
                ctxAddTrueHere.Visible = false;
            }
            else
            {
                ctxEdit.Visible = false;
                ctxBold.Visible = false;
                ctxUppercase.Visible = false;
                ctxStrike.Visible = false; // NEW
                ctxDelete.Visible = false;
                ctxLock.Visible = false;
                ctxDup.Visible = false;
                ctxFront.Visible = false;
                ctxBack.Visible = false;
                ctxBoxOn.Visible = false;
                ctxBoxOff.Visible = false;

                ctxAddHere.Visible = true;
                ctxAddTickHere.Visible = true;
                ctxAddTrueHere.Visible = true;
            }
        }

        // ===== Mouse / Inline / Pan =====
        private void Overlay_MouseDown(object sender, MouseEventArgs e)
        {
            lastMouseClient = e.Location;

            if (e.Button == MouseButtons.Right)
            {
                lastContextPoint = e.Location;
                SelectAt(e.Location);
                return;
            }

            if (spaceHeld)
            {
                panDragging = true;
                panDragStartPt = e.Location;
                panAtDragStart = pan;
                overlay.Cursor = Cursors.Hand;
                return;
            }

            if (inline.Visible) CommitInline(true);
            movedEnough = false;

            if (e.Button != MouseButtons.Left) return;

            SelectAt(e.Location);
            if (active == null || active.Locked) return;

            RectangleF rcClient = ImageRectToClient(active.ImgRect);
            RectangleF handle = new RectangleF(rcClient.Right - HANDLE, rcClient.Bottom - HANDLE, HANDLE, HANDLE);

            dragging = !handle.Contains(e.Location);
            resizing = !dragging;

            PushUndo();

            dragStartClient = e.Location;
            startRectImg = active.ImgRect;
        }

        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
            lastMouseClient = e.Location;

            if (panDragging)
            {
                int dx = e.X - panDragStartPt.X;
                int dy = e.Y - panDragStartPt.Y;
                pan = new PointF(panAtDragStart.X + dx, panAtDragStart.Y + dy);
                overlay.Invalidate();
                return;
            }

            if (active == null) return;

            if (!movedEnough && (Math.Abs(e.X - dragStartClient.X) > DRAG_THRESHOLD || Math.Abs(e.Y - dragStartClient.Y) > DRAG_THRESHOLD))
            { movedEnough = true; }

            if ((dragging || resizing) && active.Locked) return;

            if (dragging && movedEnough)
            {
                PointF a = ClientToImage(dragStartClient), b = ClientToImage(e.Location);
                if (float.IsNaN(a.X) || float.IsNaN(b.X)) return;

                RectangleF r = startRectImg; r.X += (b.X - a.X); r.Y += (b.Y - a.Y);
                if (snapToGrid) SnapRect(ref r);
                ClampToImage(ref r); active.ImgRect = r;
                overlay.Invalidate(); if (inline.Visible) PositionInlineOnActive();
            }
            else if (resizing && movedEnough)
            {
                PointF a = ClientToImage(dragStartClient), b = ClientToImage(e.Location);
                if (float.IsNaN(a.X) || float.IsNaN(b.X)) return;

                RectangleF r = startRectImg; r.Width += (b.X - a.X); r.Height += (b.Y - a.Y);
                if (r.Width < 30) r.Width = 30; if (r.Height < 12) r.Height = 12;
                if (snapToGrid) SnapRect(ref r);
                ClampToImage(ref r); active.ImgRect = r;
                overlay.Invalidate(); if (inline.Visible) PositionInlineOnActive();
            }
            else
            {
                RectangleF rc = ImageRectToClient(active.ImgRect);
                RectangleF handle = new RectangleF(rc.Right - HANDLE, rc.Bottom - HANDLE, HANDLE, HANDLE);
                overlay.Cursor = handle.Contains(e.Location) ? Cursors.SizeNWSE : (spaceHeld ? Cursors.Hand : Cursors.SizeAll);
            }
        }

        private void Overlay_MouseUp(object sender, MouseEventArgs e)
        {
            lastMouseClient = e.Location;

            if (panDragging)
            {
                panDragging = false;
                overlay.Cursor = spaceHeld ? Cursors.Hand : Cursors.IBeam;
                return;
            }

            dragging = false; resizing = false;
            overlay.Cursor = Cursors.IBeam;
        }

        // Inline editor
        private void ShowInlineForActive()
        {
            if (!(active is TextTag) || ((TextTag)active).Locked) return;
            TextTag tt = (TextTag)active;
            inline.Text = tt.Text ?? string.Empty;
            inlineOriginalText = inline.Text;
            ApplyInlineFont(tt);
            PositionInlineOnActive();
            inline.Visible = true; inline.BringToFront(); inline.Focus(); inline.SelectAll();
            UpdateLivePreview(tt);
        }

        private void ApplyInlineFont(TextTag tt)
        {
            FontStyle st = tt.Bold ? FontStyle.Bold : FontStyle.Regular;
            if (tt.Strike) st |= FontStyle.Strikeout;                     // NEW
            inline.Font = new Font(baseFont.FontFamily, Math.Max(MIN_PT, tt.FixedPt), st, GraphicsUnit.Point);
            FitRectToFont(tt);
            string wText = tt.Uppercase ? (inline.Text == null ? null : inline.Text.ToUpperInvariant()) : inline.Text;
            FitWidthToText(tt, wText);
            PositionInlineOnActive();
            UpdateLivePreview(tt);
        }

        private void PositionInlineOnActive()
        {
            if (!(active is TextTag)) return;
            RectangleF rc = ImageRectToClient(active.ImgRect);
            int pad = 1;
            int w = Math.Max(30, (int)Math.Round(rc.Width) - 2 * pad);
            int h = Math.Max(12, (int)Math.Round(rc.Height) - 2 * pad);
            int x = (int)Math.Round(rc.X) + pad;
            int y = (int)Math.Round(rc.Y) + pad;

            Rectangle bounds = overlay.ClientRectangle;
            if (x < bounds.Left) x = bounds.Left;
            if (y < bounds.Top) y = bounds.Top;
            if (x + w > bounds.Right) w = Math.Max(30, bounds.Right - x);
            if (y + h > bounds.Bottom) h = Math.Max(12, bounds.Bottom - y);

            inline.SetBounds(x, y, w, h);

            if (inline.Visible && active is TextTag ttx) UpdateLivePreview(ttx);
        }

        private void Inline_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrlMoveUndoPushed = false; e.SuppressKeyPress = true; return; }
            bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;

            if (ctrl && active is TextTag)
            {
                if (!ctrlMoveUndoPushed) { PushUndo(); ctrlMoveUndoPushed = true; }

                int step = e.Shift ? 10 : 5;
                RectangleF r = active.ImgRect; bool moved = false;
                if (e.KeyCode == Keys.Left) { r.X -= step; moved = true; }
                if (e.KeyCode == Keys.Right) { r.X += step; moved = true; }
                if (e.KeyCode == Keys.Up) { r.Y -= step; moved = true; }
                if (e.KeyCode == Keys.Down) { r.Y += step; moved = true; }
                if (moved) { if (snapToGrid) SnapRect(ref r); ClampToImage(ref r); active.ImgRect = r; PositionInlineOnActive(); overlay.Invalidate(); }
                e.SuppressKeyPress = true; return;
            }

            if (e.Alt && active is TextTag)
            {
                float delta = 0f;
                if (e.KeyCode == Keys.Up) delta = 0.5f;
                else if (e.KeyCode == Keys.Down) delta = -0.5f;

                if (delta != 0f)
                {
                    PushUndo();
                    TextTag ttAlt = (TextTag)active;
                    ttAlt.FixedPt = Math.Max(MIN_PT, ttAlt.FixedPt + delta);
                    ApplyInlineFont(ttAlt);
                    overlay.Invalidate();
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitInline(true); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; CommitInline(false); }
        }

        private void Inline_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey) { ctrlMoveUndoPushed = false; }
        }

        private void Inline_KeyPress(object sender, KeyPressEventArgs e)
        {
            bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
            if (ctrl) e.Handled = true;
        }

        private void Inline_TextChanged(object sender, EventArgs e)
        {
            if (!(inline.Visible && active is TextTag)) return;
            TextTag tt = (TextTag)active;
            string wText = tt.Uppercase ? (inline.Text == null ? null : inline.Text.ToUpperInvariant()) : inline.Text;
            FitWidthToText(tt, wText);
            PositionInlineOnActive();
            overlay.Invalidate();
            UpdateLivePreview(tt);
        }

        private void CommitInline(bool commit)
        {
            if (!inline.Visible) return;
            if (commit && active is TextTag)
            {
                TextTag tt = (TextTag)active;
                if (!string.Equals(tt.Text ?? string.Empty, inline.Text ?? string.Empty, StringComparison.Ordinal))
                {
                    PushUndo();
                    tt.Text = inline.Text;
                    string wText = tt.Uppercase ? (tt.Text == null ? null : tt.Text.ToUpperInvariant()) : tt.Text;
                    FitWidthToText(tt, wText);
                    FitRectToFont(tt);
                }
            }
            inline.Visible = false;
            livePreview.Visible = false;
            overlay.Invalidate();
        }

        private void SelectAt(Point clientPt)
        {
            active = null;
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                RectangleF rc = ImageRectToClient(tags[i].ImgRect);
                if (rc.Contains(clientPt)) { active = tags[i]; break; }
            }
            foreach (TagBase t in tags) t.Selected = (t == active);
            overlay.Invalidate();
        }

        // ===== Cut/Copy/Paste =====
        private void CopyActiveTag()
        {
            if (active == null) return;
            clipboardTag = CloneTag(active);
        }

        private void CutActiveTag()
        {
            if (active == null) return;
            clipboardTag = CloneTag(active);
            tags.Remove(active);
            active = null;
            overlay.Invalidate();
        }

        private void PasteClipboardTagAtCursor()
        {
            if (clipboardTag == null || template == null) return;
            TagBase pasted = CloneTag(clipboardTag);
            PointF pImg = ClientToImage(lastMouseClient);
            if (float.IsNaN(pImg.X))
            {
                RectangleF vp = GetViewport();
                lastMouseClient = new Point((int)(vp.X + vp.Width / 2f), (int)(vp.Y + vp.Height / 2f));
                pImg = ClientToImage(lastMouseClient);
            }
            RectangleF r = pasted.ImgRect;
            r.X = pImg.X - r.Width / 2f;
            r.Y = pImg.Y - r.Height / 2f;
            if (snapToGrid) SnapRect(ref r);
            ClampToImage(ref r);
            pasted.ImgRect = r;

            tags.Add(pasted);
            active = pasted;
            overlay.Invalidate();
        }

        private TagBase CloneTag(TagBase t)
        {
            if (t is TextTag)
            {
                TextTag tx = (TextTag)t;
                TextTag nt = new TextTag
                {
                    Text = tx.Text,
                    Uppercase = tx.Uppercase,
                    Bold = tx.Bold,
                    Strike = tx.Strike, // NEW
                    FixedPt = tx.FixedPt,
                    ImgRect = tx.ImgRect,
                    BoxCount = tx.BoxCount,
                    Locked = tx.Locked,
                    FontResolver = ResolveFont
                };
                return nt;
            }
            if (t is TickTag)
            {
                TickTag tk = (TickTag)t;
                return new TickTag { ImgRect = tk.ImgRect, Locked = tk.Locked };
            }
            if (t is PhotoTag)
            {
                PhotoTag ph = (PhotoTag)t;
                Image imgCopy = null;
                if (ph.Img != null) imgCopy = new Bitmap(ph.Img);
                return new PhotoTag { Img = imgCopy, ImgRect = ph.ImgRect, Locked = ph.Locked, CoverCrop = ph.CoverCrop };
            }
            return null;
        }

        // ===== Undo/Redo =====
        private State SnapshotState()
        {
            List<TagBase> copy = new List<TagBase>(tags.Count);
            foreach (TagBase t in tags) copy.Add(CloneTag(t));
            int idx = (active != null) ? tags.IndexOf(active) : -1;
            return new State { Tags = copy, ActiveIndex = idx };
        }

        private void RestoreState(State s)
        {
            tags.Clear();
            foreach (TagBase t in s.Tags) tags.Add(t);
            active = (s.ActiveIndex >= 0 && s.ActiveIndex < tags.Count) ? tags[s.ActiveIndex] : null;
            inline.Visible = false;
            livePreview.Visible = false;
            overlay.Invalidate();
        }

        private void PushUndo()
        {
            undoStack.Push(SnapshotState());
            redoStack.Clear();
            UpdateUndoRedoMenuEnabled();
        }

        private void DoUndo()
        {
            if (undoStack.Count == 0) return;
            State current = SnapshotState();
            State prev = undoStack.Pop();
            redoStack.Push(current);
            RestoreState(prev);
            UpdateUndoRedoMenuEnabled();
        }

        private void DoRedo()
        {
            if (redoStack.Count == 0) return;
            State current = SnapshotState();
            State next = redoStack.Pop();
            undoStack.Push(current);
            RestoreState(next);
            UpdateUndoRedoMenuEnabled();
        }

        private void UpdateUndoRedoMenuEnabled()
        {
            if (miUndo != null) miUndo.Enabled = undoStack.Count > 0;
            if (miRedo != null) miRedo.Enabled = redoStack.Count > 0;
        }

        // ===== Zoom =====
        private void Any_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == 0) return;
            if (e.Delta > 0) AdjustZoom(true, e.Location);
            else if (e.Delta < 0) AdjustZoom(false, e.Location);
        }

        private void AdjustZoom(bool zoomIn, Point? mouseClient)
        {
            float oldZoom = zoom;
            zoom = Clamp(zoom * (zoomIn ? ZOOM_STEP : 1f / ZOOM_STEP), ZOOM_MIN, ZOOM_MAX);
            if (Math.Abs(zoom - oldZoom) > 0.0001f)
            {
                overlay.Invalidate();
                if (inline.Visible) PositionInlineOnActive();
            }
        }

        private void ResetZoom()
        {
            zoom = 1.0f;
            overlay.Invalidate();
            if (inline.Visible) PositionInlineOnActive();
        }

        private static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }

        // ===== Paint/Save PNG =====
        private void Overlay_Paint(object _, PaintEventArgs e)
        {
            if (template == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF vp = GetViewport();
            if (hideTemplateImage)
            {
                using (var br = new SolidBrush(Color.White))
                    g.FillRectangle(br, vp);
            }
            else
            {
                g.DrawImage(template, vp);
            }

            if (active != null)
            {
                RectangleF a = ImageRectToClient(active.ImgRect);
                using (var p = new Pen(Color.FromArgb(80, Color.MediumPurple), 1f))
                {
                    p.DashStyle = DashStyle.Dot;
                    g.DrawLine(p, a.X + a.Width / 2f, 0, a.X + a.Width / 2f, overlay.Height);
                    g.DrawLine(p, 0, a.Y + a.Height / 2f, overlay.Width, a.Y + a.Height / 2f);
                }
            }

            foreach (TagBase t in tags)
            {
                bool showBox = !(inline.Visible && ReferenceEquals(t, active));
                t.DrawOverlay(g, ImageRectToClient(t.ImgRect), showBox);
            }
        }

        private void SaveCompositeImage()
        {
            if (inline.Visible) CommitInline(true);
            if (template == null) return;
            using (SaveFileDialog sfd = new SaveFileDialog { Title = "Save Filled Form", Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg;*.jpeg", FileName = "filled_form.png" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                using (Bitmap bmp = RenderCompositeBitmap())
                {
                    string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    ImageFormat fmt = (ext == ".jpg" || ext == ".jpeg") ? ImageFormat.Jpeg : ImageFormat.Png;
                    bmp.Save(sfd.FileName, fmt);
                }
                // NEW: save sibling layout next to image on Ctrl+S or any call
                try { SaveSiblingLayoutFor(sfd.FileName); } catch { /* ignore */ }
            }
        }

        private Bitmap RenderCompositeBitmap()
        {
            Bitmap bmp = new Bitmap(template.Width, template.Height, PixelFormat.Format24bppRgb);
            bmp.SetResolution(EXPORT_DPI, EXPORT_DPI);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                if (hideTemplateImage)
                    g.Clear(Color.White);
                else
                    g.DrawImage(template, new Rectangle(0, 0, bmp.Width, bmp.Height));
                foreach (TagBase t in tags) t.DrawFinal(g);
            }
            return bmp;
        }

        // ===== PDF Export (NO NuGet) via Microsoft Print to PDF =====
        private void SaveCompositePdf()
        {
            if (inline.Visible) CommitInline(true);
            if (template == null) { MessageBox.Show("Pehle template upload karein.", "Info"); return; }

            using (SaveFileDialog sfd = new SaveFileDialog { Title = "Save as PDF", Filter = "PDF (*.pdf)|*.pdf", FileName = "filled_form.pdf" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                using (Bitmap bmp = RenderCompositeBitmap())
                {
                    string pdfPrinter = FindPdfPrinter();
                    if (pdfPrinter == null)
                    {
                        MessageBox.Show(
                            "Windows ka \"Microsoft Print to PDF\" printer nahi mila.\n" +
                            "Windows Features me isay enable karein: Turn Windows features on or off → Print and Document Services → Microsoft Print to PDF.",
                            "PDF Printer Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (PrintDocument doc = new PrintDocument())
                    {
                        doc.PrintController = new StandardPrintController(); // no UI
                        doc.PrinterSettings.PrinterName = pdfPrinter;
                        doc.PrinterSettings.PrintToFile = true;
                        doc.PrinterSettings.PrintFileName = sfd.FileName;

                        PaperSize a4 = doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4);
                        if (a4 != null) doc.DefaultPageSettings.PaperSize = a4;

                        Image img = (Image)bmp.Clone();
                        doc.PrintPage += (s, pe) =>
                        {
                            pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                            Rectangle m = pe.MarginBounds;
                            double ir = (double)img.Width / img.Height;
                            double pr = (double)m.Width / m.Height;

                            int w, h, x, y;
                            if (ir > pr) { w = m.Width; h = (int)Math.Round(w / ir); x = m.X; y = m.Y + (m.Height - h) / 2; }
                            else { h = m.Height; w = (int)Math.Round(h * ir); y = m.Y; x = m.X + (m.Width - w) / 2; }

                            pe.Graphics.DrawImage(img, new Rectangle(x, y, w, h));
                            pe.HasMorePages = false;
                        };

                        try { doc.Print(); }
                        finally { img.Dispose(); }
                    }
                }

                try { SaveSiblingLayoutFor(sfd.FileName); } catch { /* ignore */ } // NEW: sibling .layout.xml
            }
        }

        private string FindPdfPrinter()
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                string name = p.ToLowerInvariant();
                if (name.Contains("microsoft print to pdf") || name.Contains("print to pdf") || name.Contains("pdf"))
                    return p;
            }
            return null;
        }

        // ===== Font resolve =====
        private Tuple<Font, StringFormat> ResolveFont(TextTag tag, RectangleF rect)
        {
            FontStyle style = tag.Bold ? FontStyle.Bold : FontStyle.Regular;
            if (tag.Strike) style |= FontStyle.Strikeout; // NEW
            var fmt = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            };
            return Tuple.Create(new Font(baseFont.FontFamily, Math.Max(MIN_PT, tag.FixedPt), style, GraphicsUnit.Point), fmt);
        }

        // ===== Font Size Dialog =====
        private void ShowFontSizeDialog()
        {
            float current = defaultFontPt;
            if (active is TextTag) current = ((TextTag)active).FixedPt;

            using (var dlg = new FontSizeDialog(current))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    float newPt = dlg.SelectedPointSize;
                    if (active is TextTag)
                    {
                        PushUndo();
                        TextTag tta = (TextTag)active;
                        tta.FixedPt = newPt;
                        if (inline.Visible) ApplyInlineFont(tta);
                        overlay.Invalidate();
                    }
                    if (dlg.SetAsDefault) defaultFontPt = newPt;
                }
            }
        }

        private sealed class FontSizeDialog : Form
        {
            private readonly NumericUpDown nud = new NumericUpDown
            {
                DecimalPlaces = 1,
                Minimum = 1,
                Maximum = 72,
                Increment = 0.5M,
                Width = 100,
                TextAlign = HorizontalAlignment.Right
            };
            private readonly CheckBox chkDefault = new CheckBox { Text = "Isay default bana do (mustaqbil ke labels)", AutoSize = true };
            private readonly Button btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
            private readonly Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };

            public float SelectedPointSize { get { return (float)nud.Value; } }
            public bool SetAsDefault { get { return chkDefault.Checked; } }

            public FontSizeDialog(float currentPt)
            {
                Text = "Font Size";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
                ClientSize = new Size(340, 140);

                var lbl = new Label { Text = "Size (pt):", AutoSize = true, Left = 14, Top = 18 };
                nud.Left = 90; nud.Top = 14; nud.Value = (decimal)Math.Max(1f, currentPt);

                chkDefault.Left = 14; chkDefault.Top = 52;

                btnOk.Left = 140; btnOk.Top = 90;
                btnCancel.Left = 240; btnCancel.Top = 90;

                Controls.AddRange(new Control[] { lbl, nud, chkDefault, btnOk, btnCancel });

                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }
        }

        // ===== Per-Box count dialog =====
        private void SetBoxCountForActive()
        {
            if (!(active is TextTag)) return;
            TextTag tt = (TextTag)active;

            using (Form f = new Form())
            {
                f.Text = "Set Box Count";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(260, 120);
                NumericUpDown nud = new NumericUpDown { Minimum = 1, Maximum = 200, Value = tt.BoxCount > 0 ? tt.BoxCount : 10, Left = 20, Top = 20, Width = 200 };
                Button btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 60, Top = 60, Width = 60 };
                Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 130, Top = 60, Width = 60 };
                f.Controls.AddRange(new Control[] { new Label { Text = "How many boxes?", Left = 20, Top = 2, AutoSize = true }, nud, btnOk, btnCancel });
                f.AcceptButton = btnOk; f.CancelButton = btnCancel;

                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    PushUndo();
                    tt.BoxCount = (int)nud.Value;
                    overlay.Invalidate();
                }
            }
        }

        // ===== Help Shortcuts =====
        private void ShowShortcuts()
        {
            MessageBox.Show(
                "Farhad City Hub Chowki Software\n" +
                "Shortcuts:\n" +
                "• Upload Template: Ctrl+O | Insert Photo: Ctrl+I | Passport: Ctrl+Shift+I\n" +
                "• Print A4: Ctrl+P | Save PNG: Ctrl+S (ya Ctrl+Shift+P) | Quick Save: F12\n" +
                "• Undo/Redo: Ctrl+Z / Ctrl+Y\n" +
                "• Font Size Dialog: Ctrl+Shift+F | Bold: Ctrl+B | Uppercase: context menu | Strikethrough: Ctrl+K\n" +
                "• Insert ✔: Ctrl+T | Insert TRUE: Ctrl+Shift+T\n" +
                "• Cut/Copy/Paste: Ctrl+X / Ctrl+C / Ctrl+V | Duplicate: Ctrl+D | Lock: Ctrl+L\n" +
                "• Zoom: Ctrl+Wheel | Ctrl+'+' | Ctrl+'-' | Reset: Ctrl+0\n" +
                "• Pan: Space + Drag\n" +
                "• Inline: Ctrl hold = Move; Arrow=5px; Shift+Arrow=10px\n" +
                "• Per-Box mode: context menu → Set Box Count\n" +
                "• Snap to Grid: G / View menu\n" +
                "• Live Preview Toggle: F2",
                "Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ===== Mapping: Image <-> Client =====
        private RectangleF GetViewport()
        {
            if (template == null) return RectangleF.Empty;
            Size cs = overlay.ClientSize;
            float sx = (float)cs.Width / template.Width;
            float sy = (float)cs.Height / template.Height;
            float baseScale = Math.Min(sx, sy) * zoom;
            float w = template.Width * baseScale, h = template.Height * baseScale;

            float x = (cs.Width - w) / 2f + pan.X;
            float y = (cs.Height - h) / 2f + pan.Y;
            return new RectangleF(x, y, w, h);
        }
        private RectangleF ImageRectToClient(RectangleF imgRect)
        {
            RectangleF vp = GetViewport(); if (vp.IsEmpty) return RectangleF.Empty;
            float scale = vp.Width / template.Width;
            return new RectangleF(vp.X + imgRect.X * scale, vp.Y + imgRect.Y * scale, imgRect.Width * scale, imgRect.Height * scale);
        }
        private PointF ClientToImage(Point client)
        {
            RectangleF vp = GetViewport(); if (!vp.Contains(client)) return new PointF(float.NaN, float.NaN);
            float scale = vp.Width / template.Width;
            return new PointF((client.X - vp.X) / scale, (client.Y - vp.Y) / scale);
        }

        // ======== TEXT AUTO-SIZE (MEASURE AT 300 DPI) ========
        private int MeasureLinePxAtExportDpi(float pt, bool bold)
        {
            using (Font f = new Font(baseFont.FontFamily, Math.Max(MIN_PT, pt),
                        bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point))
            using (Bitmap tmp = new Bitmap(1, 1))
            {
                tmp.SetResolution(EXPORT_DPI, EXPORT_DPI);
                using (Graphics g = Graphics.FromImage(tmp))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    SizeF sz = g.MeasureString("Mg", f, int.MaxValue, StringFormat.GenericDefault);
                    return (int)Math.Ceiling(sz.Height);
                }
            }
        }
        private int MeasureWidthPxAtExportDpi(string text, float pt, bool bold)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            using (Font f = new Font(baseFont.FontFamily, Math.Max(MIN_PT, pt),
                        bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point))
            using (Bitmap tmp = new Bitmap(1, 1))
            {
                tmp.SetResolution(EXPORT_DPI, EXPORT_DPI);
                using (Graphics g = Graphics.FromImage(tmp))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
                    {
                        SizeF sz = g.MeasureString(text, f, int.MaxValue, sf);
                        return (int)Math.Ceiling(sz.Width);
                    }
                }
            }
        }
        private void FitRectToFont(TextTag tt)
        {
            int linePx = MeasureLinePxAtExportDpi(tt.FixedPt, tt.Bold) + 2; // padding
            RectangleF r = tt.ImgRect;
            r.Height = Math.Max(12, linePx);
            if (snapToGrid) SnapRect(ref r);
            ClampToImage(ref r);
            tt.ImgRect = r;
        }
        private void FitWidthToText(TextTag tt, string text)
        {
            if (tt.BoxCount > 0) return;
            int wPx = MeasureWidthPxAtExportDpi(text ?? "", tt.FixedPt, tt.Bold) + 4; // padding
            RectangleF r = tt.ImgRect;
            r.Width = Math.Max(30, wPx);
            if (snapToGrid) SnapRect(ref r);
            ClampToImage(ref r);
            tt.ImgRect = r;
        }
        private void SnapRect(ref RectangleF r)
        {
            r.X = (float)Math.Round(r.X / SNAP_STEP_IMG) * SNAP_STEP_IMG;
            r.Y = (float)Math.Round(r.Y / SNAP_STEP_IMG) * SNAP_STEP_IMG;
            r.Width = (float)Math.Round(r.Width / SNAP_STEP_IMG) * SNAP_STEP_IMG;
            r.Height = (float)Math.Round(r.Height / SNAP_STEP_IMG) * SNAP_STEP_IMG;
        }
        private void ClampToImage(ref RectangleF r)
        {
            if (template == null) return;
            if (r.X < 0) r.X = 0;
            if (r.Y < 0) r.Y = 0;
            if (r.Right > template.Width) r.Width = template.Width - r.X;
            if (r.Bottom > template.Height) r.Height = template.Height - r.Y;
            if (r.Width < 30) r.Width = 30;
            if (r.Height < 12) r.Height = 12;
        }
        private static void DrawHandle(Graphics g, RectangleF r)
        {
            using (var b = new SolidBrush(Color.Red))
                g.FillRectangle(b, r.Right - HANDLE, r.Bottom - HANDLE, HANDLE, HANDLE);
        }

        // ===== Save/Load Layout (XML) =====
        [Serializable]
        public class SavedLayout
        {
            public int TemplateWidth;
            public int TemplateHeight;
            public List<Item> Items = new List<Item>();
            public class Item
            {
                public string Kind;
                public RectangleF Rect;
                public bool Locked;
                public string Text;
                public bool Bold;
                public bool Uppercase;
                public bool Strike; // NEW
                public float Pt;
                public int BoxCount;
                public bool CoverCrop;
                public byte[] PhotoBytes;
            }
        }

        private void SaveLayoutXml()
        {
            if (template == null) { MessageBox.Show("Template pehle load karein.", "Info"); return; }
            using (SaveFileDialog sfd = new SaveFileDialog { Title = "Save Layout", Filter = "Layout (*.xml)|*.xml", FileName = "layout.xml" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                SavedLayout lay = new SavedLayout { TemplateWidth = template.Width, TemplateHeight = template.Height };

                foreach (TagBase t in tags)
                {
                    SavedLayout.Item it = new SavedLayout.Item { Rect = t.ImgRect, Locked = t.Locked };
                    if (t is TextTag)
                    {
                        TextTag tx = (TextTag)t;
                        it.Kind = "Text"; it.Text = tx.Text; it.Bold = tx.Bold; it.Uppercase = tx.Uppercase; it.Strike = tx.Strike; it.Pt = tx.FixedPt; it.BoxCount = tx.BoxCount;
                    }
                    else if (t is TickTag) { it.Kind = "Tick"; }
                    else if (t is PhotoTag)
                    {
                        PhotoTag ph = (PhotoTag)t;
                        it.Kind = "Photo"; it.CoverCrop = ph.CoverCrop;
                        if (ph.Img != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                ph.Img.Save(ms, ImageFormat.Png);
                                it.PhotoBytes = ms.ToArray();
                            }
                        }
                    }
                    lay.Items.Add(it);
                }

                XmlSerializer xs = new XmlSerializer(typeof(SavedLayout));
                using (FileStream fs = File.Create(sfd.FileName))
                {
                    xs.Serialize(fs, lay);
                }
            }
        }

        private void LoadLayoutXml()
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Title = "Load Layout", Filter = "Layout (*.xml)|*.xml" })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                LoadLayoutXmlFrom(ofd.FileName);
            }
        }

        private void LoadLayoutXmlFrom(string path) // helper for sibling & manual load
        {
            XmlSerializer xs = new XmlSerializer(typeof(SavedLayout));
            SavedLayout lay;
            using (FileStream fs = File.OpenRead(path)) lay = (SavedLayout)xs.Deserialize(fs);

            if (template == null)
            {
                MessageBox.Show("Template image pehle load karein (same size recommended).", "Tip");
            }

            tags.Clear();
            foreach (SavedLayout.Item it in lay.Items)
            {
                if (it.Kind == "Text")
                {
                    TextTag tx = new TextTag
                    {
                        ImgRect = it.Rect,
                        Locked = it.Locked,
                        Text = it.Text,
                        Bold = it.Bold,
                        Uppercase = it.Uppercase,
                        Strike = it.Strike, // NEW
                        FixedPt = it.Pt <= 0 ? defaultFontPt : it.Pt,
                        BoxCount = it.BoxCount,
                        FontResolver = ResolveFont
                    };
                    tags.Add(tx);
                }
                else if (it.Kind == "Tick")
                {
                    tags.Add(new TickTag { ImgRect = it.Rect, Locked = it.Locked });
                }
                else if (it.Kind == "Photo")
                {
                    Image img = null;
                    if (it.PhotoBytes != null && it.PhotoBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(it.PhotoBytes))
                        {
                            img = Image.FromStream(ms);
                        }
                    }
                    tags.Add(new PhotoTag { ImgRect = it.Rect, Locked = it.Locked, Img = img, CoverCrop = it.CoverCrop });
                }
            }
            active = null; inline.Visible = false; livePreview.Visible = false; overlay.Invalidate();
        }

        // Auto-save sibling .layout.xml for any saved output path
        private void SaveSiblingLayoutFor(string imageOrPdfPath)
        {
            if (template == null) return;
            string xmlPath = Path.ChangeExtension(imageOrPdfPath, ".layout.xml");

            var lay = new SavedLayout { TemplateWidth = template.Width, TemplateHeight = template.Height };
            foreach (TagBase t in tags)
            {
                var it = new SavedLayout.Item { Rect = t.ImgRect, Locked = t.Locked };
                if (t is TextTag tx)
                {
                    it.Kind = "Text";
                    it.Text = tx.Text; it.Bold = tx.Bold; it.Uppercase = tx.Uppercase; it.Strike = tx.Strike;
                    it.Pt = tx.FixedPt; it.BoxCount = tx.BoxCount;
                }
                else if (t is TickTag) it.Kind = "Tick";
                else if (t is PhotoTag ph)
                {
                    it.Kind = "Photo"; it.CoverCrop = ph.CoverCrop;
                    if (ph.Img != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            ph.Img.Save(ms, ImageFormat.Png);
                            it.PhotoBytes = ms.ToArray();
                        }
                    }
                }
                lay.Items.Add(it);
            }

            XmlSerializer xs = new XmlSerializer(typeof(SavedLayout));
            using (FileStream fs = File.Create(xmlPath)) xs.Serialize(fs, lay);
        }

        private void PrintCompositeFlow(PaperKind paperKind)
        {
            if (inline.Visible) CommitInline(true);
            if (template == null) { MessageBox.Show("Pehle template upload karein.", "Info"); return; }

            lastPaperKind = paperKind;

            using (Bitmap bmp = RenderCompositeBitmap())
            using (PrintDocument doc = new PrintDocument())
            using (PrintDialog dlg = new PrintDialog
            {
                UseEXDialog = true,
                AllowSomePages = false,
                AllowSelection = false,
                Document = doc
            })
            {
                doc.PrintController = new StandardPrintController(); // silent

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                doc.PrinterSettings = dlg.PrinterSettings;

                PaperSize ps = FindPaperSize(doc.PrinterSettings, paperKind)
                               ?? doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault();
                if (ps != null) doc.DefaultPageSettings.PaperSize = ps;

                Image img = (Image)bmp.Clone();
                doc.PrintPage += (s, pe) =>
                {
                    pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                    Rectangle m = pe.MarginBounds;
                    double ir = (double)img.Width / img.Height;
                    double pr = (double)m.Width / m.Height;

                    int w, h, x, y;
                    if (ir > pr) { w = m.Width; h = (int)Math.Round(w / ir); x = m.X; y = m.Y + (m.Height - h) / 2; }
                    else { h = m.Height; w = (int)Math.Round(h * ir); y = m.Y; x = m.X + (m.Width - w) / 2; }

                    pe.Graphics.DrawImage(img, new Rectangle(x, y, w, h));
                    pe.HasMorePages = false;
                };

                try { doc.Print(); }
                finally { img.Dispose(); }
            }
        }


        private PaperSize FindPaperSize(PrinterSettings settings, PaperKind kind)
        {
            foreach (PaperSize p in settings.PaperSizes)
                if (p.Kind == kind) return p;
            return null;
        }

        private void SaveCompositeImageTo(string path)
        {
            using (Bitmap bmp = RenderCompositeBitmap())
            {
                bmp.Save(path, ImageFormat.Png); // hamesha PNG
            }
            try { SaveSiblingLayoutFor(path); } catch { /* ignore */ }
        }

        private void SaveCompositePdfTo(string path, PaperKind paperKind)
        {
            using (Bitmap bmp = RenderCompositeBitmap())
            {
                string pdfPrinter = FindPdfPrinter();
                if (pdfPrinter == null)
                {
                    MessageBox.Show(
                        "Windows ka \"Microsoft Print to PDF\" printer nahi mila.\n" +
                        "Windows Features se enable karein: Print and Document Services → Microsoft Print to PDF.",
                        "PDF Printer Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (PrintDocument doc = new PrintDocument())
                {
                    doc.PrintController = new StandardPrintController();
                    doc.PrinterSettings.PrinterName = pdfPrinter;
                    doc.PrinterSettings.PrintToFile = true;
                    doc.PrinterSettings.PrintFileName = path;

                    PaperSize ps = FindPaperSize(doc.PrinterSettings, paperKind);
                    if (ps != null) doc.DefaultPageSettings.PaperSize = ps;

                    Image img = (Image)bmp.Clone();
                    doc.PrintPage += (s, pe) =>
                    {
                        pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                        Rectangle m = pe.MarginBounds;
                        double ir = (double)img.Width / img.Height;
                        double pr = (double)m.Width / m.Height;

                        int w, h, x, y;
                        if (ir > pr) { w = m.Width; h = (int)Math.Round(w / ir); x = m.X; y = m.Y + (m.Height - h) / 2; }
                        else { h = m.Height; w = (int)Math.Round(h * ir); y = m.Y; x = m.X + (m.Width - w) / 2; }

                        pe.Graphics.DrawImage(img, new Rectangle(x, y, w, h));
                        pe.HasMorePages = false;
                    };

                    try { doc.Print(); }
                    finally { img.Dispose(); }
                }
            }
            try { SaveSiblingLayoutFor(path); } catch { /* ignore */ }
        }
        private void SaveCompositePdfDirect(string path, PaperKind paperKind)
        {
            using (Bitmap bmp = RenderCompositeBitmap())
            {
                GetPageSizePoints(paperKind, out int pageW, out int pageH);

                byte[] jpg = EncodeJpeg(bmp, 90L);

                const int margin = 36;
                double innerW = pageW - 2.0 * margin;
                double innerH = pageH - 2.0 * margin;
                double ir = (double)bmp.Width / bmp.Height;
                double pr = innerW / innerH;

                double drawW, drawH;
                if (ir > pr) { drawW = innerW; drawH = drawW / ir; }
                else { drawH = innerH; drawW = drawH * ir; }
                double x = margin + (innerW - drawW) / 2.0;
                double y = margin + (innerH - drawH) / 2.0;

                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var bw = new BinaryWriter(fs, System.Text.Encoding.ASCII))
                {
                    List<long> xref = new List<long>();

                    void W(string s) => bw.Write(System.Text.Encoding.ASCII.GetBytes(s));
                    void NL() => W("\n");
                    string F(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                    W("%PDF-1.4\n%"); bw.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }); NL();

                    xref.Add(fs.Position);
                    W("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

                    xref.Add(fs.Position);
                    W("2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");

                    xref.Add(fs.Position);
                    W($"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageW} {pageH}] ");
                    W("/Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n");

                    string content =
                        "q\n" +
                        $"{F(drawW)} 0 0 {F(drawH)} {F(x)} {F(y)} cm\n" +
                        "/Im0 Do\n" +
                        "Q\n";
                    byte[] contentBytes = System.Text.Encoding.ASCII.GetBytes(content);

                    xref.Add(fs.Position);
                    W("4 0 obj\n");
                    W($"<< /Length {contentBytes.Length} >>\nstream\n");
                    bw.Write(contentBytes);
                    W("endstream\nendobj\n");

                    xref.Add(fs.Position);
                    W("5 0 obj\n");
                    W($"<< /Type /XObject /Subtype /Image /Width {bmp.Width} /Height {bmp.Height} ");
                    W("/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode ");
                    W($"/Length {jpg.Length} >>\nstream\n");
                    bw.Write(jpg);
                    W("\nendstream\nendobj\n");

                    long xrefPos = fs.Position;
                    W($"xref\n0 {xref.Count + 1}\n");
                    W("0000000000 65535 f \n");
                    foreach (long off in xref)
                        W(off.ToString("0000000000", System.Globalization.CultureInfo.InvariantCulture) + " 00000 n \n");

                    W("trailer\n");
                    W($"<< /Size {xref.Count + 1} /Root 1 0 R >>\n");
                    W("startxref\n");
                    W(xrefPos.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
                    W("%%EOF");
                }
            }
            try { SaveSiblingLayoutFor(path); } catch { /* ignore */ }
        }

        private static byte[] EncodeJpeg(Image img, long quality)
        {
            var enc = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var ep = new EncoderParameters(1))
            using (var ms = new MemoryStream())
            {
                ep.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                img.Save(ms, enc, ep);
                return ms.ToArray();
            }
        }

        private static void GetPageSizePoints(PaperKind kind, out int w, out int h)
        {
            switch (kind)
            {
                case PaperKind.A4: w = 595; h = 842; break;
                case PaperKind.Legal: w = 612; h = 1008; break;
                case PaperKind.Letter: w = 612; h = 792; break;
                default: w = 612; h = 792; break;
            }
        }

        // ===== Live Preview helper =====
        private void UpdateLivePreview(TextTag tt)
        {
            if (!livePreviewOn || !inline.Visible) { livePreview.Visible = false; return; }

            string txt = tt.Uppercase ? (inline.Text ?? "").ToUpperInvariant() : (inline.Text ?? "");
            float shownPt = Math.Max(16f, tt.FixedPt * 2f);
            using (Font f = new Font(baseFont.FontFamily, shownPt, tt.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point))
            {
                FontStyle add = FontStyle.Regular;
                if (tt.Strike) add |= FontStyle.Strikeout; // NEW
                livePreview.Font = new Font(f, f.Style | add);
                livePreview.Text = txt.Length == 0 ? " " : txt;
            }

            Size r = livePreview.GetPreferredSize(Size.Empty);
            int x = inline.Left;
            int y = Math.Max(0, inline.Top - r.Height + 2 - 8);
            livePreview.Location = new Point(x, y);
            livePreview.Visible = true;
        }
    }

    internal sealed class OverlayPanel : Panel
    {
        public OverlayPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            Cursor = Cursors.IBeam;
            TabStop = true;
        }
    }

}
