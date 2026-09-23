namespace StandUpHero;

public sealed class SettingsWindow : Form
{
    public Preferences Result { get; private set; }
    public event EventHandler? ExitRequested;
    private readonly NumericUpDown sitting = new() { Minimum = 1, Maximum = 240 }, standing = new() { Minimum = 1, Maximum = 240 };
    private readonly CheckedListBox days = new() { CheckOnClick = true };
    private readonly TextBox slots = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox top = new() { Text = "Manter o personagem sempre visível", AutoSize = true }, sound = new() { Text = "Som ao mudar de postura", AutoSize = true };
    private readonly PostureAudio audio = new();
    private static readonly DayOfWeek[] DayOrder = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
    public SettingsWindow(Preferences current, Func<Preferences, bool>? apply = null)
    {
        Result = current;
        Text = "Sua rotina · StandUp Hero";
        Icon = AppAssets.Icon;
        ClientSize = new Size(480, 610); Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        void LabelAt(string text, int y) => Controls.Add(new Label { Text = text, Location = new Point(22, y), AutoSize = true });
        LabelAt("Um ritmo que combina com você", 18);
        LabelAt("Sentado (minutos)", 58); sitting.SetBounds(275, 54, 135, 30); sitting.Value = current.SittingMinutes;
        LabelAt("Em pé (minutos)", 96); standing.SetBounds(275, 92, 135, 30); standing.Value = current.StandingMinutes;
        sitting.AccessibleName = "Minutos sentado"; standing.AccessibleName = "Minutos em pé";
        LabelAt("Dias da rotina", 140); days.SetBounds(22, 169, 185, 172); days.AccessibleName = "Dias da rotina";
        days.Items.AddRange(["Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo"]);
        for (int i = 0; i < 7; i++) days.SetItemChecked(i, current.Days.Contains(DayOrder[i]));
        Controls.Add(new Label { Text = "Horários (um por linha)", Location = new Point(226, 140), AutoSize = true });
        slots.SetBounds(226, 169, 184, 108); slots.AccessibleName = "Horários, início e fim separados por hífen";
        slots.Text = string.Join(Environment.NewLine, current.Slots.Select(s => $"{s.Start:hh\\:mm}-{s.End:hh\\:mm}"));
        Controls.Add(new Label { Text = "Exemplo:\n09:00-12:00\n13:00-18:00", Location = new Point(226, 283), AutoSize = true });
        top.Location = new Point(22, 355); top.Checked = current.AlwaysOnTop;
        sound.Location = new Point(22, 387); sound.Checked = current.Sound;
        Controls.Add(new Label { Text = "Salvar reinicia o período. O reset é aplicado na hora.\nFechar esta tela mantém o mascote e os lembretes ativos.", Location = new Point(22, 425), AutoSize = true, Font = new Font("Segoe UI", 9) });
        var resetStatus = new Label { Location = new Point(22, 467), AutoSize = true, Font = new Font("Segoe UI", 9) };
        Controls.Add(resetStatus);
        var save = new Button { Text = "Salvar rotina", Location = new Point(278, 482), Size = new Size(132, 32) };
        var cancel = new Button { Text = "Cancelar", Location = new Point(160, 482), Size = new Size(108, 32), DialogResult = DialogResult.Cancel };
        var character = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(22, 54), Width = 175, AccessibleName = "Personagem" };
        character.Items.AddRange(["Homem", "Mulher"]); character.SelectedItem = current.Character;
        var preview = new PixelScene { Location = new Point(240, 26), Size = new Size(140, 96), Female = current.Character == "Mulher", BackColor = Color.Magenta };
        // A transparent preview sits on a neutral tile within settings.
        preview.Preview = true;
        character.SelectedIndexChanged += (_, _) => { preview.Female = character.Text == "Mulher"; preview.Invalidate(); };
        var standSound = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(22, 221), Width = 250, AccessibleName = "Som ao levantar" };
        var sitSound = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(22, 304), Width = 250, AccessibleName = "Som ao sentar" };
        standSound.Items.AddRange(Preferences.SoundChoices); sitSound.Items.AddRange(Preferences.SoundChoices);
        standSound.SelectedItem = current.StandSound; sitSound.SelectedItem = current.SitSound;
        var hearStand = new Button { Text = "Ouvir", AccessibleName = "Ouvir som ao levantar", Location = new Point(289, 219), Size = new Size(100, 32) };
        var hearSit = new Button { Text = "Ouvir", AccessibleName = "Ouvir som ao sentar", Location = new Point(289, 302), Size = new Size(100, 32) };
        hearStand.Click += (_, _) => audio.Play(standSound.Text);
        hearSit.Click += (_, _) => audio.Play(sitSound.Text);
        void UpdateSoundControls()
        {
            standSound.Enabled = sitSound.Enabled = sound.Checked;
            hearStand.Enabled = sound.Checked && standSound.Text != "Sem som";
            hearSit.Enabled = sound.Checked && sitSound.Text != "Sem som";
            if (!sound.Checked) audio.Play("Sem som");
        }
        sound.CheckedChanged += (_, _) => UpdateSoundControls();
        standSound.SelectedIndexChanged += (_, _) => UpdateSoundControls();
        sitSound.SelectedIndexChanged += (_, _) => UpdateSoundControls();
        UpdateSoundControls();
        var reset = new Button { Text = "Restaurar rotina padrão", Location = new Point(22, 388), Size = new Size(230, 30) };
        reset.Click += (_, _) =>
        {
            var defaults = current.WithDefaultRoutine();
            if (apply is not null && !apply(defaults)) return;
            current = defaults;
            Result = defaults;
            audio.Play("Sem som");
            sitting.Value = defaults.SittingMinutes; standing.Value = defaults.StandingMinutes;
            for (int i = 0; i < 7; i++) days.SetItemChecked(i, defaults.Days.Contains(DayOrder[i]));
            slots.Text = string.Join(Environment.NewLine, defaults.Slots.Select(s => $"{s.Start:hh\\:mm}-{s.End:hh\\:mm}"));
            resetStatus.Text = "Padrão aplicado: 45/15 min · seg–sex · 09:00–18:00.";
        };
        save.Click += (_, _) =>
        {
            var windows = new List<TimeSlot>();
            foreach (var line in slots.Lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split('-');
                if (parts.Length != 2 || !TimeSpan.TryParseExact(parts[0].Trim(), "hh\\:mm", null, out var start) || !TimeSpan.TryParseExact(parts[1].Trim(), "hh\\:mm", null, out var end) || start >= end || end >= TimeSpan.FromDays(1))
                { MessageBox.Show(this, "Use horários como 09:00-12:00, com o fim depois do início. Para virar a noite, crie horários separados em cada dia.", "Confira os horários"); return; }
                windows.Add(new(start, end));
            }
            var selected = Enumerable.Range(0, 7).Where(days.GetItemChecked).Select(i => DayOrder[i]).ToArray();
            if (selected.Length == 0 || windows.Count == 0) { MessageBox.Show(this, "Selecione pelo menos um dia e um horário."); return; }
            Result = new Preferences { SittingMinutes = (int)sitting.Value, StandingMinutes = (int)standing.Value, Days = selected, Slots = windows, AlwaysOnTop = top.Checked, Sound = sound.Checked, Activity = current.Activity, Character = character.Text, StandSound = standSound.Text, SitSound = sitSound.Text };
            if (apply is not null && !apply(Result)) return;
            DialogResult = DialogResult.OK;
        };
        AcceptButton = save; CancelButton = cancel;
        Controls.AddRange([sitting, standing, days, slots, top, reset]);
        var routinePage = new TabPage("Rotina") { UseVisualStyleBackColor = true };
        var characterPage = new TabPage("Personagem e sons") { UseVisualStyleBackColor = true };
        foreach (var control in Controls.Cast<Control>().ToArray()) routinePage.Controls.Add(control);
        sound.Location = new Point(22, 153);
        characterPage.Controls.AddRange([character, preview, sound, standSound, sitSound, hearStand, hearSit,
            new Label { Text = "Personagem", Location = new Point(22, 25), AutoSize = true },
            new Label { Text = "Ao levantar", Location = new Point(22, 194), AutoSize = true },
            new Label { Text = "Ao sentar", Location = new Point(22, 277), AutoSize = true },
            new Label { Text = "Escolha um som para cada mudança de postura.\nUse Ouvir para experimentar antes de salvar.", Location = new Point(22, 366), AutoSize = true }]);
        var tabs = new TabControl { Location = new Point(12, 12), Size = new Size(456, 530) };
        tabs.TabPages.AddRange([routinePage, characterPage]);
        save.Location = new Point(336, 560); cancel.Location = new Point(218, 560);
        var exit = new Button { Text = "Encerrar aplicativo", Location = new Point(12, 560), Size = new Size(185, 32) };
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Controls.AddRange([tabs, exit, save, cancel]);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) audio.Dispose();
        base.Dispose(disposing);
    }
}
