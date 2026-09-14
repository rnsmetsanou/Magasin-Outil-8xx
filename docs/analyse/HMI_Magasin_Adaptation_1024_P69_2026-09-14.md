# HMI magasin — adaptation 1024 × 768 et réutilisation P6.9

Date : 14 septembre 2026. Statut : proposition révisée, maquette simulée. Aucun changement du dépôt, aucune validation sur machine.

Ce complément révise la recommandation de renderer et la disposition de la proposition V0.1. Les données métier et les réserves de cette proposition restent applicables. Architecture V1.3 et les décisions d’architecture V0.2 restent normatives ; ce document ne les modifie pas.

## 1. Conclusion

Décision utilisateur confirmée : **Avalonia pour la première version de la HMI**. Blazor local reste une évolution possible à plus long terme. Cette décision remplace la recommandation Blazor de la révision précédente.

P6.9 reste utile pour reprendre les contrats applicatifs, les modèles de présentation, les langues/unités et les règles de gouvernance. Les composants Razor ne se réutilisent pas directement comme contrôles Avalonia ; leur comportement et leur identité visuelle peuvent guider des composants Avalonia. La réutilisation effective des bibliothèques C# devra être vérifiée dans le dépôt, sans dépendance au renderer web.

L’application magasin doit pouvoir être livrée avec une composition locale réduite. Il n’est pas nécessaire d’attendre la finalisation de Fleet, des passerelles, de l’audit ou de toute la démo.

## 2. Dimensions et redimensionnement

Formats confirmés par l’utilisateur : **Width="1024" Height="768"**, paysage 4:3 ; et **FIP1000 24 pouces Full HD, 1920 pixels de large × 1080 pixels de haut**, paysage 16:9. La largeur et la hauteur ne sont pas interchangeables. La surface utile dépend également de la mise à l’échelle du système et des barres de fenêtre.

Exigence proposée : le parcours principal doit être utilisable dans une surface applicative de 1024 × 768 pixels logiques. Si 1024 × 768 désigne la résolution physique de l’écran, la recette devra aussi mesurer la surface réellement disponible. Le mode plein écran et le facteur d’échelle sont donc des paramètres de qualification.

| Surface disponible | Disposition proposée |
| --- | --- |
| 1024 × 768 | Sélecteur de racks compact, places du rack choisi à gauche, fiche à droite ; accès broche/préparé en haut |
| Largeur supérieure à 1150 pixels logiques | Vue générale avec miniatures des positions, puis sélection agrandie du rack |
| FIP1000 24 pouces, 1920 × 1080 | Même organisation, davantage d’espace et de positions visibles ; aucune réduction des cibles tactiles |
| Largeur inférieure à 760 pixels logiques | Panneaux empilés pour consultation et fenêtre réduite ; ceci n’étend pas la qualification industrielle sous le minimum demandé |

Les seuils sont des choix de maquette à ajuster à la police réelle. Les numéros miniatures sont informatifs ; ils ne deviennent pas de petits boutons tactiles. La sélection se fait par rack puis par place agrandie. Les boutons ont une cible d’au moins 44 pixels logiques ; la taille physique et l’usage avec les gants éventuels devront être vérifiés sur le poste.

Le redimensionnement doit conserver la sélection, le brouillon et le suivi d’opération. Il ne doit ni relancer l’observation, ni reconnecter la machine, ni soumettre une commande. Une modification du contexte langue/unité pendant une saisie doit être différée ou traitée explicitement, pour ne pas réinterpréter silencieusement une valeur saisie.

Le pavé numérique est resserré sur quatre colonnes. Pendant l’édition, l’identité outil/place reste visible et les informations secondaires cèdent la place aux champs. Le clavier alphabétique du poste reste à qualifier. En cas de hauteur réduite par le clavier, l’application produit devra garder le champ actif et les actions accessibles.

Les résultats de recherche nombreux devront être paginés ou limités à une liste défilante dans l’application. La maquette affiche actuellement tous les résultats et peut donc dépasser la hauteur nominale dans ce cas. Elle ne constitue pas une qualification complète du 1024 × 768.

## 3. Ce que P6.9 apporte

Les statuts ci-dessous sont rapportés par le fichier de contexte, pas réexécutés ici.

| Élément | Réutilisation envisagée | Limite |
| --- | --- | --- |
| Application locale Blazor | Référence de comportement et de style ; migration future possible | Aucun composant Razor importé directement dans Avalonia |
| PresentationContext, PocLocalizationRuntime | Langue et culture de présentation | Extraire la partie réutilisable sans importer les scénarios de démonstration |
| PhysicalValuePresentationProjector | Affichage des grandeurs et conversion d’unités | L’affichage d’un axe en mm/in ne définit pas les unités des correcteurs Beckhoff |
| États qualité/fraîcheur/provenance | Indiquer quand une valeur peut être utilisée | Une connexion ADS fraîche ne garantit pas une valeur FANUC fraîche |
| Autorisation, licence et admission | Décision d’accepter une demande côté service | Un bouton désactivé ne remplace pas le contrôle d’autorité |
| Cycle des opérations et OperationLifecycle | Suivi de la demande jusqu’à sa preuve métier | Vue opérateur concise ; identifiants et détails dans une vue dédiée |
| Simulator | Développement des parcours, conflits et pertes de liaison | Compléter avec un modèle du magasin, pas seulement PrepareTool |
| Adaptateur Beckhoff existant | Examiner connexion, observation et cycle de session | Chemin Beckhoff intégré encore non vérifié d’après le contexte |
| Audit ED-6 | Prévoir les corrélations utiles | En préparation ; ne pas le présenter comme disponible et validé |

ED-5A est déclaré vérifié localement. ED-5B est implémenté mais son exécution locale reste attendue ; ED-5 global est rouvert. La préparation d’un outil FANUC a une preuve locale dans NC Guide, mais cela ne valide ni le chargement physique du magasin ni les correcteurs de cette machine.

## 4. Frontières techniques à conserver

Les composants visuels consomment des modèles de présentation et des services applicatifs. Les demandes de modification passent par l’autorité applicative, puis par le domaine magasin et le mapping propre à la machine. L’accès ADS (Automation Device Specification, protocole Beckhoff) reste sous la couche Technology.

Le renderer ne référence ni le client ADS ni les structures automate. Le domaine magasin distingue outil, place affectée, position physique, correcteurs, observation et opération. Il conserve une identité stable lors d’une modification ; le numéro de place ne tient pas lieu d’identité outil.

Le service machine conserve la session et le suivi des opérations indépendamment de la page. Une fermeture ou reconnexion de l’interface ne doit pas rejouer une écriture. Une interface Blazor ne doit donc pas posséder la connexion machine dans son état de page ou son circuit utilisateur. Pour la livraison autonome, privilégier un service machine dont le cycle de vie peut être séparé de l’hôte de présentation ; arrêter l’interface ne doit pas arrêter une opération en cours.

Blazor dispose de plusieurs modes de rendu. En Interactive Server, l’interactivité repose sur une connexion au serveur. Un serveur sur le même PC permet un déploiement sans Internet, mais une perte de cette liaison locale reste un défaut de disponibilité de l’interface. Le mode exact du projet doit être vérifié avant réutilisation. Sources : [modes de rendu Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0), [modèles d’hébergement Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0).

Le chemin local ne dépend pas d’OPC UA ou de Fleet. Le flux FOCAS de préparation FANUC de la démo ne remplace pas le chemin prévu PC → Beckhoff → FANUC de ce magasin.

La migration future doit principalement remplacer la composition et les implémentations des services. Les modèles métier, règles de saisie et composants visuels pourront être conservés si leurs contrats sont alignés après inspection. Aucune compatibilité binaire n’est présumée à ce stade.

## 5. Améliorations visuelles

La maquette reprend WM Blue #202945 et Machine blue #385CAD, avec variantes claires/sombres. Les couleurs d’état restent accompagnées de textes ou symboles. Une sélection bleue indique la sélection, sans suggérer que l’outil est disponible ou qu’une opération a réussi.

Le nom Plateforme Multi-Technologie apparaît sans .NET. L’écran reste nommé Gestion des outils. Le logo WM original extrait de Architecture.pptx est intégré en haut à gauche dans la maquette, sans déformation, sur un fond blanc assurant sa lisibilité dans les deux thèmes. La version produit pourra utiliser l’actif officiel du dépôt. La police Poppins devra être embarquée localement ; la maquette utilise une police de repli si elle n’est pas installée. Aucun téléchargement de police n’est requis par la maquette.

Les fonctions de démonstration des profils, licences, passerelles et technologies ne sont pas nécessaires dans le parcours courant du magasin. Un utilisateur opérateur consulte son contexte et ses actions disponibles ; il ne modifie pas la licence produit au moyen d’un bouton de démonstration.

Pour une future opération réelle, les libellés proposés sont : demande en cours, demande refusée avec motif, acceptée et en attente de confirmation machine, terminée avec preuve, résultat indéterminé à vérifier. Une écriture technique réussie ne déclenche pas automatiquement le message « modification appliquée ».

Le thème Light (clair) est le thème initial de cette révision. Un sélecteur Clair/Sombre permet de comparer les deux apparences ; il reste indépendant du thème de la conversation. Les surfaces, textes, champs, sélections et couleurs d’état changent ensemble, sans modifier le brouillon ni les données. Dans Avalonia, prévoir des ressources sémantiques pour les deux thèmes et une préférence utilisateur persistée.

## 6. Points qui restent à résoudre sur la machine

- Symboles de broche et d’outil préparé, et structures ST_Manipulator_Head, ST_Changing_Arm, ST_Miscellaneous.
- Propriétaire de chaque donnée, fréquence réelle de copie FANUC vers Beckhoff, cas de la mesure et des correcteurs en broche.
- Unités, facteurs d’échelle, limites, codes et correspondance entre les différentes représentations de correcteurs.
- Protocole d’écriture ciblée, acquittement, refus, preuve d’application et traitement des changements concurrents.
- Encodage et capacité en octets du nom d’outil, pas seulement sa longueur affichée.
- Intégration du mode plein écran, clavier tactile, mise à l’échelle et redémarrage sur le PC cible.

La possibilité technique d’appeler PrepareTool dans la démo ne suffit pas à inclure une commande de mouvement dans le premier périmètre magasin. Son inclusion exige une définition de parcours, de préconditions et de preuve propres à la machine.

## 7. Suite recommandée

1. Inspecter les contrats applicatifs et bibliothèques de présentation réutilisables de p6-7-live-commissioning-impl, indépendamment du renderer web.
2. Réaliser la première tranche Avalonia magasin sur données simulées, avec logo WM, thèmes Light/Dark et disposition paysage adaptative aux deux formats confirmés.
3. Qualifier consultation, édition, recherche, clavier et redimensionnement à 1024 × 768 et à la résolution réelle du 24 pouces, en thèmes clair/sombre.
4. Brancher la lecture Beckhoff et vérifier la provenance/fraîcheur des données avant les écritures.
5. Ajouter une première écriture ciblée après définition et vérification du protocole d’application.

Critères complémentaires : aucune soumission lors du changement de taille/langue, brouillon préservé, refus expliqué, perte de liaison visible, aucune répétition automatique et absence de faux succès après reconnexion.

## 8. Réalisation de cette révision

Le fragment interactif a été révisé : racks compacts, disposition adaptative, pavé numérique sur quatre colonnes, palette WM et correction du rafraîchissement des résultats après un changement de nom. Les valeurs et confirmations restent simulées. Le dépôt de la plateforme n’a pas été modifié.

Vérification exécutée : lecture du fragment modifié et compilation syntaxique JavaScript réussie. La recette navigateur n’a pas pu être exécutée : moteur Chromium absent et téléchargement inaccessible dans l’environnement. Aucun résultat de hauteur, de débordement ou de parcours interactif exécuté n’est revendiqué. Le respect complet du 1024 × 768, notamment avec clavier ouvert et recherche longue, reste à mesurer dans le renderer Avalonia puis sur le poste réel.

La nouvelle maquette conversationnelle présente un cadre paysage 4:3 à largeur standard et 16:9 sur une grande surface, sans écraser les contrôles. Sur une surface étroite, le contenu peut se réorganiser et augmenter la hauteur ; cet aperçu ne reproduit donc pas à lui seul les dimensions physiques du poste. Elle reste une illustration interactive, pas une application Avalonia exécutée.
