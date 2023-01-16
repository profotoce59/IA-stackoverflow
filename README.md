# Projet de prédiction de labels stackoverflow.

Dans ce projet nous avons d'abord récupérer plusieurs centaines de posts provenant du forum stackoverflow grâce à l'API de stackexchange. Nous avons communiquer avec l'API en utilisant un script javascript.

Dans un premier temps nous avons essayer de faire une baseline (fichier baseline.ipynb). Nous avons fais deux tests différents, un premier dans lequelle, on recherche si on trouve les tags les plus souvent utilisé dans le titre environ 5 (java, c , js) et nous obtenons une performance de 0.12. Ensuite nous avons fais une seconde baseline où on a cherché à retrouver tous les tags disponible et nous obtenons une performance de 0.5 (indicateur de performance qui regarde si un seul tag prédit est dans la liste de tag attendus donc ce n'est pas ka performance exacte).
Dans un deuxième temps nous avons dû nettoyer la donnée. C'est à dire, enlever la ponctuation inutile, les mots inutile (mots liant, certains verbe qui ne permettent pas de prédire le sujet du commentaire.)

Nous avons ensuite créer un dictionnaire de mots (bag of word). Ensuite nous avons tokenizer les mots (pour que chaque mot possède une clé correspondante).

<img src="stack_overflow.jpg" width="350" height="200">

Pour réaliser un random Forest, nous avons fait le TF-IDF qui consiste à travailler avec la fréquence des mots qui reviennent le plus souvent pour un label défini.
Malhereusement, pour le random forest, nous ne pouvons prédire qu'un seul label à la fois. Nous avons donc choisi d'essayer de prédire pour chaque commentaire un seul label.
Il reste une possibilité de prédire plusieurs labels, mais cela fait augmenter exponentiellement le nombre de classe différentes en sortie de notre arbre de décision.

<img src="Random_Forest.png" width="350" height="200">
