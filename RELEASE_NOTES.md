# ValPlay — Notas da versão 1.0

**Reprodutor de mídia para central VW Play**

---

## Visão geral

O **ValPlay** é um reprodutor de áudio e vídeo desenvolvido para a central multimídia **VW Play** (Android Automotive 9). Esta primeira versão substitui o player padrão com suporte ampliado a formatos, navegação por pastas, playlist e configurações que persistem entre o uso do veículo.

**Versão:** 1.0  
**Plataforma:** Android Automotive 9.0 (API 28)  
**Pacote:** `com.valplay.media`

---

## Novidades

### Biblioteca de mídia

- Varredura de arquivos em pendrive, cartão SD e armazenamento externo
- Seleção da origem de busca (pasta raiz do scan)
- **Navegação por pastas** — entre nas subpastas e volte com o botão ◀
- **Reprodução por pasta** — toque em ▶▶ para criar uma playlist com todos os arquivos da pasta (incluindo subpastas)
- **Reprodução individual** — toque em ▶ ou na faixa para tocar um arquivo; a playlist usa os demais arquivos da pasta atual
- **Busca** — encontre músicas e vídeos por título ou nome do arquivo em toda a biblioteca escaneada

### Player de áudio

- Layout otimizado para a área útil da central:
  - **Capa do álbum** à esquerda (extraída do arquivo quando disponível)
  - **Informações da faixa** no centro: título, artista, álbum, ano e duração
  - **Controles de reprodução** centralizados: aleatório, anterior, play/pause, próximo e repetir
- **Barra de progresso em largura total** na parte inferior, com tempo decorrido e tempo total
- Leitura de metadados (ID3 e equivalentes): artista, álbum, ano, duração e capa embutida

### Player de vídeo

- Reprodução com **ExoPlayer** (via MediaElement)
- Modo **tela cheia** com botão dedicado; botão Voltar sai da tela cheia
- Controles de playlist e barra de progresso integrados

### Opções de reprodução

- **Modo aleatório** (shuffle)
- **Repetir**: desligado, repetir faixa ou repetir playlist
- **Playlist automática** ao tocar pasta, arquivo ou resultado de busca
- Avançar e voltar faixa no player

### Configurações

- Ativar/desativar modo aleatório (mantido após desligar o carro)
- Escolher modo de repetição
- **Retomar de onde parou** — ao ligar o veículo, continua a mesma faixa na posição salva
- Limpar sessão salva

### Integração com o veículo

- Interface adaptada à área útil **1332 × 636** px (DPI fixo 160), em paisagem
- Alvos de toque amplos, adequados ao uso no carro
- **Foco de áudio** — pausa quando outro app assume o áudio e pode retomar quando o foco volta
- **Notificação de reprodução** no sistema Android
- Tema escuro alinhado à interface da central

### Formatos suportados

**Áudio:** MP3, FLAC, WAV, AAC, M4A, OGG, Opus, WMA, APE, ALAC, AMR, 3GP  

**Vídeo:** MP4, MKV, AVI, MOV, WMV, FLV, WebM, M4V, TS, MPEG, MPG, 3GP

*A compatibilidade real pode variar conforme codecs e perfil do arquivo.*

### Sobre

- Tela com versão do app, data da compilação e identificação da plataforma VW Play

---

## Requisitos

- Central **VW Play** com Android Automotive 9.0 (API 28)
- Arquitetura **ARM64** (build Release)
- Armazenamento externo com arquivos de mídia (pendrive ou cartão SD)
- Permissão de leitura de armazenamento na primeira varredura

---

## Como começar

1. Conecte o pendrive ou cartão SD com suas mídias
2. Abra a aba **Biblioteca**, escolha a origem e toque em **Atualizar**
3. Navegue pelas pastas ou use a busca
4. Toque em uma faixa ou use ▶▶ em uma pasta para começar
5. Ajuste shuffle, repetição e retomada em **Ajustes**

---

## Observações conhecidas

- Metadados (artista, álbum, ano, capa) dependem das tags gravadas no arquivo; faixas sem tags exibem valores padrão
- Após atualizar o app, pode ser necessário um novo **Atualizar** na biblioteca para recarregar metadados de arquivos já escaneados
- O app não utiliza Google Mobile Services (GMS)

---

*ValPlay v1.0 — desenvolvido para uso na central multimídia VW Play.*
