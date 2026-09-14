# Maintenance V1 — matrice de consultation

Statut : lecture seule validée ; sélection et présentation détaillées proposées. Analyse statique du 14 septembre 2026, sans modification PLC ni application, sans essai sur machine.

Référence : Magasin-Outil-8xx, commit ccf2e5761608eb4fd79ffbfcaa310a4cc7ad1467. Les empreintes Git des dix fichiers PLC principaux examinés localement ont été comparées à celles du dépôt à cette révision : identiques.

## 1. Frontière de responsabilité

La nouvelle interface homme-machine (HMI) et l’exposition OPC UA consultent les réglages et états de maintenance utiles. Aucun bouton de sauvegarde de réglages, teach, changement de mode, mouvement de pince, lancement de contrôle ou remise à zéro mémoire n’est exposé en V1. La gestion des outils, correcteurs et opérations Préparer/Charger conserve son périmètre métier distinct.

Les symboles ci-dessous sont des sources candidates pour l’adaptateur Beckhoff, pas des noms de nœuds OPC UA définitifs. Leur disponibilité effective via ADS, leur fréquence de mise à jour et leur comportement sur la machine installée restent à vérifier.

## 2. Valeur configurée et valeur appliquée

`GV_HMI.stMagazineSettings` est la structure de réglage de l’interface Beckhoff. `PRG_Tool_Magazine_Type1_Refresh` recopie et limite plusieurs de ses champs dans `GV_Tool_Magazine`.

Proposition : afficher prioritairement la valeur appliquée côté magasin ; proposer la valeur configurée en détail lorsque la comparaison est utile. Une différence n’est pas automatiquement une panne : elle peut provenir d’une limite, d’une sauvegarde non appliquée ou d’une particularité du programme. Ne pas inventer un indicateur « sauvegardé » à partir de la seule égalité des valeurs.

La lecture de ces deux structures ne garantit pas une acquisition atomique. Les données conservent leur qualité, leur fraîcheur et leur provenance ; une comparaison doit tenir compte de leur instant d’acquisition.

## 3. Réglages du magasin

Préfixes : **H** = `GV_HMI.stMagazineSettings` ; **M** = `GV_Tool_Magazine`. Top/Bottom désignent les parties supérieure/inférieure, conservées séparément dans chaque champ.

| Famille et champs exacts | Source proposée | Type / unité | Traitement |
|---|---|---|---|
| `iMaxRelRackTop`, `iMaxRelRackBottom` | M ; H en détail | INT, nombre | Nombre de racks appliqué ; ne pas confondre maximum matériel et configuration active |
| `iMaxRelRackLocationTop`, `iMaxRelRackLocationBottom` | M ; H en détail | INT, nombre | Nombre de places configurées par rack ; tenir compte des places interdites pour la capacité utile |
| `lrXLocationOffsetTop/Bottom`, `lrYLocationOffsetTop/Bottom`, `lrZLocationOffsetTop/Bottom` | M ; H en détail | LREAL, longueur ; unité native à confirmer | Espacements entre emplacements ; ne pas supposer qu’un LREAL implique des millimètres |
| `lrXFirstLocationTop/Bottom`, `lrYLocationTop/Bottom`, `lrZLowestRackTop/Bottom` | M ; H en détail | LREAL, longueur ; unité native à confirmer | Origines et positions géométriques configurées |
| `lrZAbsCompensationLocationTop/Bottom` | M et H séparément | LREAL, mm explicitement commentés | La routine examinée affecte zéro à M ; la valeur H ne doit pas être affichée comme appliquée |
| `lrYOffsetFrontLocation`, `lrZOffsetAboveLocation` | M et H séparément | LREAL, longueur ; unité à confirmer | Mapping sous réserve d’anomalie décrite en section 7 |
| `lrZOffsetAboveArmGripper1`, `lrZOffsetAboveArmGripper2` | M ; H en détail | LREAL, longueur ; unité à confirmer | Dégagement au-dessus des positions de transfert |
| `lrZOffsetToolCheck`, `lrYOffsetToolCheck` | H ou M après traçage du consommateur | LREAL, longueur ; unité à confirmer | Données candidates de détail ; statut « appliqué » non établi par cette inspection |
| `rZBrakeTimer` | H ou M après traçage du consommateur | REAL, durée ; unité et source effectives non établies | Ne pas afficher arbitrairement secondes ou millisecondes |

Les notations `Top/Bottom` de ce tableau désignent deux champs distincts, jamais un symbole contenant une barre oblique. La configuration matérielle de référence compte 5 racks supérieurs et 3 inférieurs ; ces valeurs ne doivent pas être figées dans le contrat commun.

## 4. Pince et manipulateur

| Information | Source | Sémantique et limite |
|---|---|---|
| Durée configurée d’ouverture / fermeture | `GV_HMI.stManipulatorGripper.iMouvementOpen`, `.iMouvementClose` | INT ; PRG_HMI les convertit en TIME puis multiplie par 1000. L’interprétation en secondes est déduite de cette conversion ; à confirmer sur banc avant contrat d’unité définitif |
| Durée convertie | `GV_HMI.stManipulatorGripper.tMouvementOpen`, `.tMouvementClose` | TIME ; paramètre de temporisation, pas mesure de durée réelle du mouvement |
| Option de contrôle après ouverture / fermeture | `GV_HMI.stManipulatorGripper.bCheckSensorOpen`, `.bCheckSensorClose` | BOOL de configuration ; ce ne sont pas des retours capteurs et ils ne prouvent pas l’état ouvert/fermé |
| Présence détectée au cône | `GV_IO.diToolPresenceCone` | Signal utilisé par FB_Manipulator_Gripper pour ses contrôles ; câblage et disponibilité à confirmer ; ne prouve pas à lui seul la position mécanique de la pince |
| Présence mémorisée | `GV_Tool_Magazine.stManipulator.bToolPresenceMemory` | Mémoire logique distincte du signal de présence physique |
| Positions du manipulateur | `GV_Tool_Magazine.stManipulator.lrXPosition`, `.lrYPosition`, `.lrZPosition` | LREAL ; déclaration examinée, origine des valeurs et unités à tracer avant de les présenter comme positions mesurées |
| Autorisations manuelles PLC | `GV_Tool_Magazine.stManipulator.bAuthorizationManuGripperOpen`, `.bAuthorizationManuGripperClose` | État calculé par le PLC ; ne donne aucun droit utilisateur et n’active aucun bouton dans la V1 |

Le mode manuel inspecté force `bGripperSensor := FALSE` avant appel de `fbGripperManu`, alors que des cycles automatiques transmettent les options configurées. Un affichage global « contrôle capteur actif » serait donc trompeur. Présenter « option configurée » et conserver le contexte du mode si l’on expose l’usage effectif.

`ENUM_Gripper_Mode` décrit une demande Open/Close utilisée par le bloc, pas une preuve de position physique. Ne pas en dériver un voyant « pince ouverte confirmée ».

## 5. Modes et états utiles

| Information | Source | Contrat de lecture proposé |
|---|---|---|
| Mode courant | `GV_OpMode.eOpMode` | Enum natif : None=0, AUTO=1, MANU=2, INIT=3 ; traductions HMI proposées : Aucun, Automatique, Manuel, Initialisation |
| Conditions PLC permettant les modes | `GV_OpMode.bInitAllowed`, `.bAutoAllowed`, `.bManuAllowed` | Booléens diagnostiques ; ne sont ni des permissions de plateforme ni une commande de changement de mode |
| Contrôle magasin terminé | `GV_Tool_Magazine.bCheckFullMagazineDone`, `.bCheckOneMagazineDone` | Candidats à confirmer par traçage du cycle ; sans identifiant/date de contrôle, TRUE ne prouve pas qu’un contrôle vient d’être effectué |
| Contrôle de casse en cours | `GV_Tool_Magazine.stManipulator.bCtrlBreakage` | État de processus candidat ; valider son alimentation et sa portée |
| Connexion et fraîcheur | Contrats Machine de plateforme | Exposées indépendamment du mode et des valeurs ; ne pas remplacer une valeur indisponible par zéro ou FALSE |

`eOpModeRequest` est une demande et reste exclue des écritures V1. Les autorisations de teach disponibles dans le PLC peuvent éventuellement compléter un diagnostic après validation de leur utilité, sans exposer les actions associées.

## 6. Présentation et contrat partagé proposés

Trois groupes de consultation suffisent pour commencer : **Configuration du magasin**, **Pince et manipulateur**, **Mode et diagnostics**. Les informations secondaires vont dans une vue de détail ; aucune longue page mélangeant les réglages aux actions outil.

Chaque donnée doit avoir : clé sémantique stable, valeur typée, nature (configuration, valeur appliquée, observation ou mémoire), unité explicite si établie, qualité, fraîcheur, provenance et horodatages disponibles. Aucun horodatage source n’est fabriqué. La préférence mm/pouces du client n’altère pas les unités du contrat publié aux autres clients.

Les droits de consultation sont évalués côté services. Les champs de maintenance restent en lecture seule même pour un administrateur. Aucun mécanisme générique d’écriture de structure ne doit permettre de contourner cette restriction. Les libellés des booléens distinguent « option », « autorisation PLC », « demande » et « état observé ».

## 7. Constats à soumettre à l’équipe automatisme

Ces points sont des constats de code, pas des défauts confirmés sur la machine installée. Aucune correction n’a été appliquée.

1. **Affectation Y/Z suspecte.** Dans PRG_Tool_Magazine_Type1_Refresh, deux lignes successives affectent `GV_Tool_Magazine.lrYOffsetFrontLocation`, d’abord depuis le champ HMI Y, puis depuis `GV_HMI.stMagazineSettings.lrZOffsetAboveLocation`. Confirmer l’intention et la révision utilisée en production ; ne pas corriger silencieusement le mapping côté HMI.
2. **Compensations forcées à zéro.** La routine affecte zéro à `lrZAbsCompensationLocationTop/Bottom` ; la source HMI est commentée. Confirmer si c’est volontaire et si ces champs doivent être visibles comme réglages inactifs.
3. **Contrôle capteur différent selon parcours.** Le chemin manuel impose FALSE ; les parcours automatiques inspectés transmettent les options. Confirmer les libellés attendus et ce qu’il est utile d’afficher.
4. **Unités.** Confirmer les longueurs hors commentaires explicites, la durée des entiers de pince, l’unité de rZBrakeTimer et la convention des positions.
5. **Fraîcheur des états.** Définir la durée de validité et la portée des résultats de contrôle ; identifier les retours physiques réellement disponibles pour la pince.
6. **Valeur active.** Confirmer le cycle de sauvegarde/application et les sources autoritatives pour les réglages encore non tracés.

## 8. Recette à prévoir

- Lire les mêmes valeurs sémantiques depuis la HMI et OPC UA, avec mêmes unités de référence, nature et qualité.
- Modifier un réglage depuis la HMI Beckhoff : observer la valeur configurée puis, si applicable, la valeur appliquée sans les confondre.
- Perdre la connexion : signaler la perte de fraîcheur sans fabriquer une position, un mode ou un résultat de contrôle.
- Vérifier l’absence d’écriture de maintenance via les deux clients, y compris avec droits administrateur.
- Vérifier qu’une consultation n’active aucun déclencheur de sauvegarde, de contrôle ou de mouvement.
- Qualifier séparément les unités et la cohérence temporelle sur banc ; aucun de ces essais n’a été réalisé ici.

## 9. Sources du dépôt

Tous les chemins ci-dessous sont relatifs à la racine PLC `Tool_Magazine_8xx/PLC_Tool_Magazine_8xx/`, à la révision indiquée en tête.

- `13_HMI/02_GVLs/GV_HMI.TcGVL`
- `13_HMI/01_DUTs/02_ST/ST_HMI_Magazine_Settings.TcDUT`
- `13_HMI/01_DUTs/02_ST/ST_HMI_Manipulator_Gripper.TcDUT`
- `13_HMI/03_POUs/01_PRGs/PRG_HMI.TcPOU`
- `09_Tool_Magazine/03_POUs/01_PRGs/PRG_Tool_Magazine_Type1_Refresh.TcPOU`
- `09_Tool_Magazine/03_POUs/01_PRGs/PRG_Tool_Magazine_Type1.TcPOU`
- `09_Tool_Magazine/03_POUs/02_FBs/FB_Manipulator_Gripper.TcPOU`
- `09_Tool_Magazine/01_DUTs/02_ST/ST_Manipulator_Head.TcDUT`
- `03_Operating_Mode/02_GVLs/GV_OpMode.TcGVL`
- `03_Operating_Mode/01_DUTs/01_ENUM/ENUM_OpMode.TcDUT`

Compléments : GV_Tool_Magazine.xml fourni et appels de pince dans PRG_Cycle.TcPOU. Les déclarations seules n’attestent pas d’une valeur mise à jour en fonctionnement.
