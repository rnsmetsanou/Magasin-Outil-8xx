# Magasin 8xx — contrat d’exposition V0.1

Date : 14 septembre 2026. Statut : proposition fonctionnelle, sans modification de code.

## 1. Décisions et portée

L’accord utilisateur suivant l’analyse de déploiement est retenu comme validation des quatre profils proposés : Local isolé, Intégration usine, Rattaché au parc, Intégration complète. Aucun profil par défaut, lieu d’installation du collecteur Fleet ou transport entre processus n’est fixé par cet accord.

La première version doit exposer des données et actions via OPC UA (Open Platform Communications Unified Architecture). La connexion à des équipements OPC UA viendra ensuite. L’interface homme-machine (HMI) web et OPC UA doivent utiliser les mêmes services applicatifs gouvernés ; Fleet conserve sa responsabilité actuelle de supervision en lecture seule.

Cette version précise le périmètre candidat et les points à valider. Elle ne fige ni identifiants de nœuds OPC UA, ni signatures de méthodes, ni codes d’énumérations, ni unités natives non documentées.

## 2. Sources examinées

Cahier des charges Gestion table outil, version 1.1 du 17 août 2026 ; Structures.txt ; GV_Tool_Magazine.xml ; ST_Tool_Location.xml ; ST_Tool_Parameters.xml ; ST_Tool_Name.xml ; ST_Tool_Corrector_M.xml ; ST_Tool_Corrector_T.xml ; ENUM_Tool_Type.xml.

Référence plateforme : analyse de déploiement du 14 septembre 2026, fondée sur PlateformeWM-Demo à bc0819d0774c67e920481a7e0312d12f55a291a8. Cette étape relit les pièces métier ; elle ne constitue pas une nouvelle inspection exhaustive du programme PLC (automate programmable).

Le cahier des charges demande explicitement création/modification des données d’outils au magasin et modification des données de l’outil en broche. Il demande également Secure ADS pour le connecteur et la prise en charge des structures de réglage. Il affecte teach, modes magasin, messages et alarmes à une HMI Beckhoff. La capacité technique de lire/écrire une structure ne suffit donc pas à décider quelle interface doit exposer chaque commande.

## 3. Matrice de lecture proposée

| Domaine | Source identifiée | HMI et OPC UA proposés | Point à confirmer |
|---|---|---|---|
| Identité, connexion, disponibilité | Contrats communs plateforme | Identité stable, connexion, qualité/fraîcheur, capacités et raisons d’indisponibilité | Identité d’installation et données lisibles selon permissions |
| Structure du magasin | GV_Tool_Magazine, ST_Tool_Location | Racks, emplacements absolus/relatifs, configuration active et restrictions | Cohérence entre configuration relative et table réelle |
| Présence et occupation | bToolPresence, bUsed, bEnable, bForbiden, bBlocked | Faits séparés ; présence observée distincte d’une affectation logique | Propriétaire et règle de mise à jour de chaque indicateur |
| Contrôle de casse | bBreakageOk | Résultat avec contexte connu | Validité temporelle et signification d’un FALSE sans contrôle récent |
| Identification outil | ST_Tool_Name, Number, Location, FixedLocation | Nom, numéro, place affectée et informations de localisation validées | Unicité, encodage du nom, sens des champs et doublons éventuels |
| Paramètres | ST_Tool_Parameters | Durée de vie, vitesse maximale, dimensions et autres paramètres documentés | Échelles, unités, enums et règles d’écriture |
| Correcteurs fraisage | ListCorrToolM[0..4] | Longueur, usure longueur, rayon, usure rayon, PositionW | Sens de PositionW, correcteurs applicables et mise à l’échelle |
| Correcteurs tournage | ListCorrToolT[0..4] | Longueur, usure, rayon, PositionW, Y/Z, quadrant | Sens de LontZ, unités et quadrant ; pas de correction silencieuse du nom natif |
| Outil préparé et en broche | Contrats sémantiques plateforme ; mapping 8xx à compléter | Identifiant et observation indépendante de la place au magasin | Symboles autoritatifs et échanges PLC/CNC |
| Opérations | Runtime Application/Machine | Identifiants, état, motif, résultat observé | Reprise autorisée après changement de session |
| Alarmes | Plateforme et intégration Beckhoff à préciser | Lecture proposée selon données réellement disponibles | Événements couverts et partage avec HMI Beckhoff |

La machine décrite possède 140 emplacements théoriques et 137 physiques, avec 1, 2 et 96 interdits. Ce sont les valeurs de la configuration fournie, pas une constante universelle du contrat plateforme. Les numéros d’emplacement ne sont pas des numéros d’outil.

Les noms visibles tels que HSK-A/HSK-T ne doivent pas être déduits d’ENUM_Tool_Type : l’enum fourni contient Unknow, Small, Big et GPS70. Il faut conserver les concepts distincts tant que leur relation n’est pas établie.

## 4. Actions proposées pour V1

Les noms ci-dessous décrivent des cas d’usage ; ils ne constituent pas des signatures figées.

| Action | Justification | Conditions avant implémentation |
|---|---|---|
| Créer une fiche outil | Demande explicite du cahier des charges | Champs obligatoires, numéro unique, affectation et distinction avec insertion physique |
| Modifier les données d’un outil au magasin | Demande explicite | Matrice champ/propriétaire/permission, bornes, révision attendue et mécanisme d’écriture cohérente |
| Modifier les correcteurs | Structures fournies et demande de modification | Tous les champs applicables validés ; ne pas limiter arbitrairement à l’usure |
| Modifier les données de l’outil en broche | Demande explicite | Autorité PLC/CNC, échanges Profinet, conditions machine et preuve de prise en compte |
| Préparer un outil | Parcours discuté et service plateforme existant | Protocole réel 8xx, ressources, gardes et critère de complétion |
| Charger un outil en broche | Parcours discuté et sémantique plateforme | Protocole réel 8xx et preuve observée ; pas d’affectation directe de l’état « en broche » |
| Consulter une opération | Nécessaire au suivi | Contrôle de visibilité et corrélation durable selon politique retenue |

Aucune suppression de fiche, décharge physique, remise à zéro de durée de vie, commande teach, changement de mode, contrôle complet du magasin ou mouvement de pince n’est ajouté implicitement. Ces fonctions restent dans la matrice à instruire ; elles ne sont pas déclarées définitivement hors périmètre.

## 5. Règles communes proposées

- Données Machine exposées en lecture ; changements effectués par cas d’usage gouvernés. Aucun accès arbitraire aux symboles PLC dans le contrat OPC UA métier.
- Les « requêtes libres sur le même connecteur » demandées par le cahier des charges restent une capacité technique à cadrer ; elles ne deviennent pas une API publique contournant la plateforme.
- Contexte authentifié établi côté serveur, identité du client, intention stable, cible et révision attendue pour les modifications.
- Autorisation, licence, audit et préconditions réévalués avant admission. Les confirmations graphiques ne remplacent aucun de ces contrôles.
- Pas d’hypothèse d’atomicité des écritures PLC. Le protocole doit traiter modifications partielles, conflits avec la CNC et résultat incertain.
- Retour d’acceptation distinct du résultat constaté. Une écriture ADS réussie ne suffit pas à déclarer une opération physique terminée.
- Reprise par consultation/réconciliation après réponse perdue ; aucun renvoi automatique d’action sur reconnexion.
- Unités d’échange explicites et stables ; préférences mm/pouces appliquées par client. Aucun facteur de conversion inventé à partir du seul type DINT.
- Champs non documentés, notamment UseFlags, Options, ModelFlags et listes utilisateur : conserver leur existence dans l’inventaire, sans inventer leur sémantique ni autoriser leur écriture publique.
- Identités des systèmes automatisés et permissions propres à leurs actions à définir ; aucune substitution par un compte opérateur partagé.

## 6. Projection Fleet proposée

Réutiliser la projection existante : identité machine, version runtime/contrat, composants installés, capacités, synthèse des droits de licence sans secrets, dernier état de connexion, outils préparé/en broche, alarmes disponibles, âge et compatibilité du rapport.

L’export de la table complète des outils et des correcteurs n’est pas requis pour commencer le raccordement Fleet et reste à valider. La publication Fleet ne doit pas relire le PLC à chaque envoi, modifier une licence ou commander un outil.

## 7. Vérification attendue avant raccordement réel

1. Même modification depuis HMI et OPC UA : mêmes validations et résultat ; seconde modification sur ancienne révision refusée explicitement.
2. Absence de permission, licence expirée ou audit indisponible : aucun nouvel effet technologique ; consultation suivant les droits.
3. Présence physique, affectation logique et outil courant non confondus.
4. Modification en broche concurrente avec CNC : comportement documenté et vérifié.
5. Réponse perdue ou session recréée : suivi autorisé sans répétition d’action.
6. Modification partielle PLC : état conservateur, audit de ce qui est connu et procédure de réconciliation.
7. Paramètres en mm/pouces : conversion vérifiée, arrondi explicite et aucun changement silencieux des données publiées aux autres clients.
8. Fleet indisponible : fonctionnement local préservé et absence de rapports anciens présentés comme frais.

## 8. Prochaine décision métier

Le point à trancher en priorité est la responsabilité des fonctions de maintenance : teach, réglages de pince et modes magasin restent-ils uniquement dans la HMI Beckhoff pour la V1, ou doivent-ils aussi être disponibles dans la nouvelle application et son exposition OPC UA ?

Les détails du mapping PLC, des échelles et du protocole seront ensuite présentés comme questions techniques précises à l’équipe concernée, après inspection du programme complet. Aucun message à cette équipe n’est envoyé par cette analyse.
