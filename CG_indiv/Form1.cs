using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CG_indiv
{
    public partial class Form1 : Form
    {
        private List<PointF> points = new List<PointF>();
        private List<PointF> sortedPoints = new List<PointF>();
        private List<List<PointF>> stackSteps = new List<List<PointF>>(); // состояния стека по шагам
        private int stepIndex = -1;

        private PictureBox pictureBox;
        private Button btnBuild, btnNext, btnClear;
        private bool isBuilding = false;
        public Form1()
        {
            InitializeComponent();
            this.Text = "Выпуклая оболочка: метод Грэхема";
            this.ClientSize = new Size(800, 600);
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.Paint += PictureBox_Paint;

            // Кнопки
            var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
            btnBuild = new Button { Text = "Построить", Width = 100 };
            btnNext = new Button { Text = "Далее", Width = 100, Enabled = false };
            btnClear = new Button { Text = "Очистить", Width = 100 };

            btnBuild.Click += (s, e) => StartGrahamScan();
            btnNext.Click += (s, e) => NextStep();
            btnClear.Click += (s, e) => ClearAll();
            panel.Controls.Add(btnBuild);
            panel.Controls.Add(btnNext);
            panel.Controls.Add(btnClear);

            this.Controls.Add(pictureBox);
            this.Controls.Add(panel);

        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (isBuilding) return; // 🔒 нельзя добавлять точки во время/после построения

            if (e.Button == MouseButtons.Left)
            {
                points.Add(new PointF(e.X, e.Y));
                pictureBox.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                ClearAll();
            }
        }

        private void ClearAll()
        {
            points.Clear();
            sortedPoints.Clear();
            stackSteps.Clear();
            stepIndex = -1;
            isBuilding = false;

            btnBuild.Enabled = true;
            btnNext.Enabled = false;
            btnNext.Text = "Далее";
            btnClear.Enabled = true;

            pictureBox.Invalidate();
        }


        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Отрисовка всех точек
            foreach (var p in points)
            {
                g.FillEllipse(Brushes.Black, p.X - 3, p.Y - 3, 6, 6);
            }

            // 2. Текущее состояние стека (если есть)
            if (stepIndex >= 0 && stepIndex < stackSteps.Count)
            {
                var currentStack = stackSteps[stepIndex];
                if (currentStack.Count > 0)
                {
                    // Точки в стеке — зелёные
                    foreach (var p in currentStack)
                    {
                        g.FillEllipse(Brushes.Green, p.X - 4, p.Y - 4, 8, 8);
                    }

                    // Соединяем стек линиями (незамкнутая ломаная)
                    using (var pen = new Pen(Color.Red, 2))
                    {
                        for (int i = 0; i < currentStack.Count - 1; i++)
                        {
                            g.DrawLine(pen, currentStack[i], currentStack[i + 1]);
                        }
                    }
                }

                if (stepIndex == stackSteps.Count - 1 && currentStack.Count >= 3)
                {
                    using (var pen = new Pen(Color.DarkRed, 2.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(pen, currentStack.Last(), currentStack.First()); // замыкаем
                    }
                }
            }

            // 3. Сортированные точки (серые), кроме p0
            if (sortedPoints.Count > 0 && stepIndex == -1) // до запуска — показываем порядок
            {
                var p0 = sortedPoints[0];
                g.FillEllipse(Brushes.Blue, p0.X - 5, p0.Y - 5, 10, 10); // стартовая — синяя

                for (int i = 1; i < sortedPoints.Count; i++)
                {
                    var p = sortedPoints[i];
                    g.FillEllipse(Brushes.Gray, p.X - 3, p.Y - 3, 6, 6);
                    // Можно добавить номер для наглядности:
                    g.DrawString(i.ToString(), Font, Brushes.Gray, p.X + 6, p.Y - 6);
                }
            }
        }

        // === Алгоритм Грэхема с записью шагов ===
        private void StartGrahamScan()
        {
            if (points.Count < 3)
            {
                MessageBox.Show("Нужно минимум 3 точки!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 1. Выбрать экстремальную точку S0 — самая нижняя (и самая левая при равенстве y)
                PointF s0 = points[0];
                foreach (var p in points)
                {
                    // «Ниже» означает: больше Y, или при равенстве Y — меньше X
                    if (p.Y > s0.Y || (p.Y == s0.Y && p.X < s0.X))
                        s0 = p;
                }
                // 2. Сортировка остальных точек по полярному углу относительно S0;
                //    при равенстве угла — по возрастанию расстояния (ближняя раньше)
                var rest = points.Where(p => p != s0).ToList();
                rest.Sort((a, b) =>
                {
                    double angleA = Math.Atan2(a.Y - s0.Y, a.X - s0.X);
                    double angleB = Math.Atan2(b.Y - s0.Y, b.X - s0.X);

                    int angleCmp = angleA.CompareTo(angleB);
                    if (angleCmp != 0)
                        return angleCmp;

                    // Углы равны → сравниваем по расстоянию до S0 (меньше расстояние — раньше)
                    double distA = DistanceSq(s0, a);
                    double distB = DistanceSq(s0, b);
                    return distA.CompareTo(distB);
                });

                var sorted = new List<PointF> { s0 };
                sorted.AddRange(rest);

                // 3–4. Обход с использованием стека (точно по вашему описанию)
                stackSteps.Clear();
                var stack = new Stack<PointF>();

                // Добавляем первые две точки (S0 и S1) — они всегда в оболочке
                stack.Push(sorted[0]);
                stack.Push(sorted[1]);

                // Сохраняем начальное состояние (2 точки)
                stackSteps.Add(stack.Reverse().ToList());

                // Обрабатываем точки с i = 2 до конца (S2, S3, ...)
                for (int i = 2; i < sorted.Count; i++)
                {
                    PointF nextPoint = sorted[i]; // Si+1 — текущая рассматриваемая точка

                    // Пока в стеке ≥ 2 точки и nextPoint НЕ слева от луча (Si-1 → Si)
                    while (stack.Count >= 2)
                    {
                        PointF si = stack.Pop();        // Si — последняя в стеке
                        PointF si_1 = stack.Peek();     // Si-1 — предпоследняя

                        // Ориентация: > 0 → левый поворот (nextPoint слева от луча si_1 → si)
                        // ≤ 0 → не слева (коллинеарна или справа) → удаляем Si
                        if (Orientation(si_1, si, nextPoint) > 0)
                        {
                            stack.Push(si); // восстанавливаем si — поворот левый, оставляем
                            break;
                        }
                        // Иначе: si удалена, продолжаем цикл (теперь проверяем луч si-2 → si-1)
                    }

                    // Добавляем nextPoint (Si+1) в стек
                    stack.Push(nextPoint);

                    // Сохраняем текущее состояние стека как шаг
                    stackSteps.Add(stack.Reverse().ToList());
                }

                // 5. Алгоритм завершён
                isBuilding = true;
                stepIndex = -1;
                btnBuild.Enabled = false;
                btnNext.Enabled = true;
                btnClear.Enabled = true;

                // Обновляем сортированный список для отрисовки порядка (если нужно)
                sortedPoints = sorted;

                pictureBox.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isBuilding = false;
            }
        }

        // Вспомогательные функции
        private static double Orientation(PointF a, PointF b, PointF c)
        {
            // Возвращает: >0 — левый поворот, <0 — правый, 0 — коллинеарны
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private static double DistanceSq(PointF a, PointF b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private void NextStep()
        {
            if (stepIndex < stackSteps.Count - 1)
            {
                stepIndex++;
                pictureBox.Invalidate();

                if (stepIndex == stackSteps.Count - 1)
                {
                    btnNext.Text = "Завершено";
                    btnNext.Enabled = false;
                }
            }
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
