namespace StandUpHero;

public sealed class SettingsWindow : Form
{
    public Preferences Result { get; private set; }
    public event EventHandler? ExitRequested;
    public event Action<bool, int>? VisualPreviewRequested;
    private readonly NumericUpDown sitting = new() { Minimum = 1, Maximum = 240 }, standing = new() { Minimum = 1, Maximum = 240 };
    private readonly CheckedListBox days = new() { CheckOnClick = true };
    private readonly TextBox slots = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox top = new() { Text = "Manter o mascote sobre as outras janelas", AutoSize = true }, sound = new() { Text = "Tocar um som ao mudar de postura", AutoSize = true };
    private readonly PostureAudio audio = new();
    private readonly Label sessionWarning = new() { Location = new Point(22, 420), Size = new Size(398, 90), ForeColor = Color.DarkRed, Visible = false,
        Text = "Não foi possível receber os avisos de bloqueio nesta\nsessão. O app tentará novamente; por enquanto,\nuse \"Pausar rotina\" antes de bloquear o computador." };
    internal void ShowSessionWarning(bool show) => sessionWarning.Visible = show;
    private static readonly DayOfWeek[] DayOrder = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
    public SettingsWindow(Preferences current, Func<Preferences, bool>? apply = null)
    {
        Result = current;
        Text = "Configurações · StandUp Hero";
        Icon = AppAssets.Icon;
        ClientSize = new Size(480, 650); Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        void LabelAt(string text, int y) => Controls.Add(new Label { Text = text, Location = new Point(22, y), AutoSize = true });
        LabelAt("Defina quando alternar entre sentado e em pé.", 18);
        LabelAt("Tempo sentado (minutos)", 58); sitting.SetBounds(275, 54, 135, 30); sitting.Value = current.SittingMinutes;
        LabelAt("Tempo em pé (minutos)", 96); standing.SetBounds(275, 92, 135, 30); standing.Value = current.StandingMinutes;
        sitting.AccessibleName = "Minutos sentado"; standing.AccessibleName = "Minutos em pé";
        LabelAt("Dias da rotina", 140); days.SetBounds(22, 169, 185, 172); days.AccessibleName = "Dias da rotina";
        days.Items.AddRange(["Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo"]);
        for (int i = 0; i < 7; i++) days.SetItemChecked(i, current.Days.Contains(DayOrder[i]));
        Controls.Add(new Label { Text = "Horários (um por linha)", Location = new Point(226, 140), AutoSize = true });
        slots.SetBounds(226, 169, 184, 108); slots.AccessibleName = "Horários, início e fim separados por hífen";
        slots.Text = string.Join(Environment.NewLine, current.Slots.Select(s => $"{s.Start:hh\\:mm}-{s.End:hh\\:mm}"));
        Controls.Add(new Label { Text = "Exemplo com pausa:\n09:00-12:00\n13:00-18:00\nVale nos dias marcados.", Location = new Point(226, 283), AutoSize = true });
        top.Location = new Point(22, 355); top.Checked = current.AlwaysOnTop;
        sound.Location = new Point(22, 387); sound.Checked = current.Sound;
        Controls.Add(new Label { Text = "Ao salvar, a rotina recomeça pelo período sentado.\n\"Restaurar rotina padrão\" aplica na hora: 45 minutos\nsentado e 15 em pé, de segunda a sexta, das 9h às 18h.\nFechar esta tela não encerra o app nem os lembretes.", Location = new Point(22, 425), AutoSize = true, Font = new Font("Segoe UI", 9) });
        var resetStatus = new Label { Location = new Point(22, 514), AutoSize = true, Font = new Font("Segoe UI", 9) };
        Controls.Add(resetStatus);
        var save = new Button { Text = "Salvar rotina", Location = new Point(278, 482), Size = new Size(132, 32) };
        var cancel = new Button { Text = "Cancelar", Location = new Point(160, 482), Size = new Size(108, 32), DialogResult = DialogResult.Cancel };
        var visual = new CheckBox { Text = "Mostrar avisos visuais", Location = new Point(22, 28), AutoSize = true, Checked = current.VisualAlerts };
        var visualSeconds = new NumericUpDown { Minimum = 5, Maximum = 30, Value = current.VisualAlertSeconds, Location = new Point(272, 78), Width = 110, AccessibleName = "Duração do balão em segundos" };
        var previewStand = new Button { Text = "Testar: levantar", Location = new Point(22, 238), Size = new Size(172, 34) };
        var previewSit = new Button { Text = "Testar: sentar", Location = new Point(210, 238), Size = new Size(172, 34) };
        previewStand.Click += (_, _) => VisualPreviewRequested?.Invoke(true, (int)visualSeconds.Value);
        previewSit.Click += (_, _) => VisualPreviewRequested?.Invoke(false, (int)visualSeconds.Value);
        void UpdateVisualControls() => visualSeconds.Enabled = previewStand.Enabled = previewSit.Enabled = visual.Checked;
        visual.CheckedChanged += (_, _) => UpdateVisualControls();
        UpdateVisualControls();
        var startWithWindows = new CheckBox { Text = "Iniciar com o Windows", Location = new Point(22, 28), AutoSize = true, Checked = current.StartWithWindows };
        var pauseOnLock = new CheckBox { Text = "Pausar ao bloquear o computador", Location = new Point(22, 230), AutoSize = true, Checked = current.PauseOnLock };
        var suggestRelaxing = new CheckBox { Text = "Sugerir \"relaxando\" após uma pausa", Location = new Point(22, 28), AutoSize = true, Checked = current.SuggestRelaxing };
        var idleMinutes = new NumericUpDown { Minimum = 2, Maximum = 60, Value = current.IdleMinutes, Location = new Point(300, 78), Width = 94, AccessibleName = "Minutos sem interação antes da sugestão" };
        idleMinutes.Enabled = suggestRelaxing.Checked;
        suggestRelaxing.CheckedChanged += (_, _) => idleMinutes.Enabled = suggestRelaxing.Checked;
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
            resetStatus.Text = "Rotina padrão restaurada e salva.";
        };
        save.Click += (_, _) =>
        {
            var windows = new List<TimeSlot>();
            foreach (var line in slots.Lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split('-');
                if (parts.Length != 2 || !TimeSpan.TryParseExact(parts[0].Trim(), "hh\\:mm", null, out var start) || !TimeSpan.TryParseExact(parts[1].Trim(), "hh\\:mm", null, out var end) || start >= end || end >= TimeSpan.FromDays(1))
                { MessageBox.Show(this, "Informe um período por linha, como 09:00-12:00. O horário final deve ser posterior ao inicial, no mesmo dia. Não são aceitos períodos que atravessam a meia-noite.", "Confira os horários"); return; }
                windows.Add(new(start, end));
            }
            var selected = Enumerable.Range(0, 7).Where(days.GetItemChecked).Select(i => DayOrder[i]).ToArray();
            if (selected.Length == 0 || windows.Count == 0) { MessageBox.Show(this, "Selecione pelo menos um dia e informe um período de horário para ativar a rotina."); return; }
            Result = new Preferences { SittingMinutes = (int)sitting.Value, StandingMinutes = (int)standing.Value, Days = selected, Slots = windows, AlwaysOnTop = top.Checked, Sound = sound.Checked, Activity = current.Activity, Character = character.Text, StandSound = standSound.Text, SitSound = sitSound.Text, VisualAlerts = visual.Checked, VisualAlertSeconds = (int)visualSeconds.Value, SuggestRelaxing = suggestRelaxing.Checked, IdleMinutes = (int)idleMinutes.Value, StartWithWindows = startWithWindows.Checked, PauseOnLock = pauseOnLock.Checked };
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
            new Label { Text = "Som para lembrar de levantar", Location = new Point(22, 194), AutoSize = true },
            new Label { Text = "Som para lembrar de sentar", Location = new Point(22, 277), AutoSize = true },
            new Label { Text = "Escolha o som de cada lembrete.\nUse \"Ouvir\" para experimentar antes de salvar.\nA opção \"Sem som\" silencia apenas aquele lembrete.", Location = new Point(22, 366), AutoSize = true }]);
        var tabs = new TabControl { Location = new Point(12, 12), Size = new Size(456, 570) };
        tabs.TabPages.AddRange([routinePage, characterPage]);
        var visualPage = new TabPage("Avisos visuais") { UseVisualStyleBackColor = true };
        visualPage.Controls.AddRange([visual, visualSeconds, previewStand, previewSit,
            new Label { Text = "Duração do balão (segundos)", Location = new Point(22, 82), AutoSize = true },
            new Label { Text = "O balão avisa quando é hora de levantar ou sentar.\nEle pulsa duas vezes sem tirar o foco da sua tarefa.\nApós o tempo definido, fica uma seta de lembrete.\nClique na seta para rever o aviso e suas opções.", Location = new Point(22, 137), Size = new Size(395, 90) },
            new Label { Text = "Os botões acima mostram um exemplo de cada aviso,\nsem tocar sons nem alterar o tempo da sua rotina.\nOs avisos continuam aparecendo se você ocultar\no mascote pelo menu.", Location = new Point(22, 300), Size = new Size(395, 100) },
            new Label { Text = "Em um aviso real, use \"Daqui a 5 minutos\" para adiar\na troca de postura. O mascote volta à postura anterior\ne avisa novamente após 5 minutos. A próxima etapa\ncomeça com sua duração completa. Os intervalos\nsalvos não mudam. Na prévia, esse botão fica inativo.", Location = new Point(22, 420), Size = new Size(395, 110) }]);
        tabs.TabPages.Add(visualPage);
        var activityPage = new TabPage("Atividade") { UseVisualStyleBackColor = true };
        activityPage.Controls.AddRange([suggestRelaxing, idleMinutes,
            new Label { Text = "Tempo sem usar mouse ou teclado (min)", Location = new Point(22, 82), AutoSize = true },
            new Label { Text = "Se você ficar esse tempo sem usar mouse ou teclado,\nao voltar verá uma sugestão silenciosa. Clique em\n\"Relaxar\" para mudar para \"Relaxando\" e parar os\nlembretes, ou em \"Continuar trabalhando\" para manter\na rotina. Ignorar a sugestão não muda seu status.", Location = new Point(22, 137), Size = new Size(398, 105) },
            new Label { Text = "Privacidade", Location = new Point(22, 248), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
            new Label { Text = "O app consulta apenas há quanto tempo você não usa\nmouse ou teclado. Não captura o que você digita,\nnem lê a tela ou os aplicativos abertos. Não grava\nhistórico nem envia dados. Para parar as consultas,\ndesmarque esta opção e clique em \"Salvar rotina\".", Location = new Point(22, 278), Size = new Size(398, 100) },
            new Label { Text = "A sugestão aparece apenas no horário da rotina,\ncom o status \"Trabalhando\" e o timer sem pausa.\nEla some após 30 segundos e só pode aparecer\nnovamente depois de 30 minutos. O app não sabe\nse você estava descansando, lendo ou em reunião.", Location = new Point(22, 402), Size = new Size(398, 110) }]);
        tabs.TabPages.Add(activityPage);
        var windowsPage = new TabPage("Windows") { UseVisualStyleBackColor = true };
        windowsPage.Controls.AddRange([startWithWindows, pauseOnLock, sessionWarning,
            new Label { Text = "Abre o mascote ao entrar na sua conta do Windows.\nEsta opção vem desativada. Marque-a e clique em\n\"Salvar rotina\" para ativar. Para remover, desmarque\ne salve novamente.\n\nMantenha o app na mesma pasta. Se você o mover,\nabra-o na nova pasta e salve esta opção novamente.", Location = new Point(22, 66), Size = new Size(398, 150) },
            new Label { Text = "Preserva o tempo restante quando você bloqueia\no computador (por exemplo, com Win+L). Ao voltar,\nescolha \"Continuar\" ou \"Manter pausado\".\n\nSe você já havia pausado a rotina, ela permanece\npausada. Os horários configurados continuam valendo;\num período encerrado não será retomado.", Location = new Point(22, 270), Size = new Size(398, 150) }]);
        tabs.TabPages.Add(windowsPage);
        save.Location = new Point(336, 600); cancel.Location = new Point(218, 600);
        var exit = new Button { Text = "Encerrar aplicativo", Location = new Point(12, 600), Size = new Size(185, 32) };
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Controls.AddRange([tabs, exit, save, cancel]);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) audio.Dispose();
        base.Dispose(disposing);
    }
}
