# 📋 PlanifKanban

**PlanifKanban** est une application WPF moderne permettant de gérer, planifier et suivre l'avancement de tâches sous forme de Kanban. Elle propose une visualisation Gantt, des filtres avancés, une sauvegarde persistante, et bien plus.

---

## 🚀 Fonctionnalités principales

- ✅ Gestion de tâches par colonnes (À faire, En cours, En test, Terminée)
- 🔀 Glisser-déposer des tâches entre colonnes
- ✏️ Création et modification avec formulaire complet
- 📅 Gestion des dates : demandée, prévue, début, finalisation
- ⏱ Estimations de temps prévues / réelles (jours/heures)
- 📊 Affichage Gantt avec échelle dynamique (Heure / Jour / Semaine / Mois)
- 🗃 Sauvegarde/chargement local en XML
- 📦 Export PDF et Excel (bientôt)
- 🔍 Tri multi-colonnes dans les tableaux
- 🧠 Visualisation intelligente dans la fenêtre d'ordonnancement (`ScheduleWindow`)
- 🎨 Interface moderne, responsive et intuitive (styles personnalisés)

---

## 🛠 Technologies

- Framework : [.NET 8](https://dotnet.microsoft.com/)
- UI : WPF (XAML, MVVM partiel)
- Langage : C#
- Persistance : XML
- Bibliothèques : 
  - `System.Windows.Controls`
  - `System.Xml.Serialization`
  - `System.Windows.Data` (CollectionView)
  - `ObservableCollection<T>`

---

## 📦 Installation

### Prérequis

- Windows 10/11
- [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Clone du dépôt

```bash
git clone https://github.com/rodrigueantunes/PlanifKanban.git
cd PlanifKanban
```

### Lancement

Ouvrez la release et exécutez.

---

## 🧭 Utilisation

### 🧱 Vue Kanban

- Double-cliquez pour modifier une tâche.
- Clic droit pour supprimer ou déplacer dans une autre colonne.
- Les tâches avec date de finalisation sont figées.
- Les couleurs changent automatiquement :
  - ✅ Vert si finalisée à temps
  - ❌ Rouge si en retard

### 📅 Vue Ordonnancement (`ScheduleWindow`)

- Accessible via le menu ou bouton dédié
- Permet de filtrer par statut
- Tri multi-colonnes
- Affiche la date "SortDate" (Date début ou prévue prioritaire)

### 📈 Vue Gantt

- Affiche toutes les tâches avec date prévue ou de début
- Exclut les tâches finalisées avec une date passée
- Double-clic sur une barre pour modifier la tâche
- Sélectionnez l'échelle de temps (Jours, Semaines, Mois)

---

## 📁 Structure du projet

```
PlanifKanban/
│
├── Models/                 # Modèles de données (TaskModel, etc.)
├── ViewModels/            # ViewModels (KanbanViewModel, etc.)
├── Views/                 # Fenêtres WPF (MainWindow, ScheduleWindow, GanttWindow)
├── Converters/            # Converters WPF (NullToVisibility, etc.)
├── Resources/             # Styles, images, ressources
├── App.xaml               # Configuration globale de l'application
├── PlanifKanban.csproj    # Fichier projet
└── README.md              # Ce fichier
```

---

## 💾 Sauvegarde et Chargement

Les tâches sont sauvegardées localement dans un fichier XML :

```csharp
kanbanViewModel.SaveToFile("kanban.xml");
KanbanViewModel loaded = KanbanViewModel.LoadFromFile("kanban.xml");
```

Les tâches sont regroupées par colonne (`TodoTasks`, `InProgressTasks`, etc.)

---

## 📌 Roadmap

- [x] Vue Kanban interactive
- [x] Ordonnancement avec tri dynamique
- [x] Affichage Gantt par échelle
- [ ] Export PDF des vues
- [ ] Export Excel

---

## 🧪 Tests

> Aucun framework de test intégré pour le moment. Des tests manuels sont effectués via les UI.

---

## 🤝 Contribuer

Les contributions sont les bienvenues !  
Merci de suivre ces étapes :

1. Fork du repo
2. Créez votre branche : `git checkout -b feature/amélioration`
3. Commit : `git commit -m "Ajout d'une fonctionnalité"`
4. Push : `git push origin feature/amélioration`
5. Pull Request 🚀

---

## 📄 Licence

Ce projet est sous licence MIT

---

## 🙌 Remerciements

- 💙 Merci à tous ceux qui ont testé et fait des retours.
- ✨ Ce projet est né pour améliorer l'organisation des équipes techniques internes.

---

## ✉️ Contact

Pour toute question, amélioration ou bug :  
📧 `rodrigue.antunes@gmail.com`

---

> Fait en WPF – Parce que le Kanban, c’est la vie.

---
