# UI copy (pt-PT) and formatting

All user-facing text is **European Portuguese (pt-PT)**. Use the strings below verbatim; if a string you need is missing, ask — do not improvise copy. Tone: direct, polite, impersonal ("Indique…", "Selecione…"), no exclamation marks, no Brazilian forms (use *ecrã*, *ficheiro*, *transferir*, *guardar*, *eliminar*, *utilizador*, *registo*).

Server error messages (ProblemDetails `title`/`detail`, validation `errors`) already come in pt-PT from the API — show them as returned; the strings here are for the UI itself and for client-side checks.

## 1. Formatting rules

Implement once in `web/` as shared helpers (`Intl` with locale `pt-PT`); never format by hand in components.

| Value | Rule | Example |
|---|---|---|
| Price | `Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 })` when the value has no cents; otherwise 2 decimals. Thousands separator is a (narrow) no-break space, symbol after the number. | `18 900 €` · `9 750 €` · `12 499,50 €` |
| Price on request | `priceOnRequest = true` → text **"Preço sob consulta"** (never "0 €", never empty) | Preço sob consulta |
| Mileage | `Intl.NumberFormat('pt-PT')` + `" km"` (no-break space before km). `0` → "0 km". | `98 000 km` |
| Year | Plain integer, no separator | `2019` |
| Engine | `{engineCc} cm³` | `1 995 cm³` |
| Power | `{powerKw} kW ({round(powerKw × 1.35962)} cv)` | `140 kW (190 cv)` |
| Doors / seats | Plain integer | `5` |
| File size | < 1 MB: `{KB} KB` (integer); otherwise `{MB} MB` with 1 decimal, pt-PT comma | `840 KB` · `1,2 MB` |
| Date | `dd/MM/yyyy` in `Europe/Lisbon` | `14/11/2026` |
| Date-time | `dd/MM/yyyy, HH:mm` in `Europe/Lisbon` | `14/11/2026, 10:42` |
| Relative (admin lists, optional) | Same day: "hoje, HH:mm"; previous day: "ontem, HH:mm"; else date | `Atualizado hoje, 10:42` |
| Counts | `Intl.PluralRules('pt-PT')`: 1 → singular, otherwise plural | `1 viatura` · `42 viaturas` · `0 viaturas` |
| Range | En dash, no spaces | `1–24 de 42` |
| Missing value in tables | Em dash `—` | — |

Number inputs (admin): accept `18900`, `18 900`, `18.900` and `18900,50`; normalise on blur to the formatted value; send a plain decimal to the API.

## 2. Enum labels (API value → UI text)

| Enum | Values |
|---|---|
| `VehicleStatus` | Draft → **Rascunho** · Published → **Publicado** (badge) / **Publicadas** (dashboard, tabs) · Sold → **Vendido** (badge) / **Vendidas** |
| `FuelType` | Petrol → Gasolina · Diesel → Gasóleo · Hybrid → Híbrido · PlugInHybrid → Híbrido plug-in · Electric → Elétrico · Lpg → GPL · Other → Outro |
| `Transmission` | Manual → Manual · Automatic → Automática |
| `Drivetrain` | FWD → Dianteira · RWD → Traseira · AWD → Integral (4×4) |
| `BodyType` | Hatchback → Utilitário · Sedan → Sedan · Wagon → Carrinha · Suv → SUV · Coupe → Coupé · Convertible → Descapotável · Mpv → Monovolume · Van → Comercial · Pickup → Pick-up · Other → Outro |
| `DocumentCategory` | RegistrationDocument → Documento único (DUA) · Inspection → Inspeção · Maintenance → Manutenção · Warranty → Garantia · Purchase → Compra · Other → Outro |
| Sort (public) | published_desc → Mais recentes · price_asc → Preço: mais baixo · price_desc → Preço: mais alto · year_desc → Ano: mais recente · year_asc → Ano: mais antigo |
| Sort (admin) | updated_desc → Última alteração · created_desc → Mais recentes · price_asc → Preço: mais baixo · price_desc → Preço: mais alto · year_desc → Ano: mais recente |
| `missingFields` (422) | make → Marca · model → Modelo · year → Ano · mileageKm → Quilómetros · fuel → Combustível · transmission → Caixa · price → Preço (ou sob consulta) · primaryImage → Foto principal |

## 3. Public site

| Where | Text |
|---|---|
| Skip link | Saltar para o conteúdo |
| Header nav | Viaturas |
| `<title>` catalogue | Viaturas usadas — SilvaStock |
| `<title>` detail | {Marca} {Modelo} {Ano} — SilvaStock |
| Catalogue H1 | Viaturas disponíveis |
| Result count | {n} viatura / {n} viaturas |
| Filters title / button | Filtros · Filtros ({n ativos}) |
| Search label / placeholder | Pesquisar · Marca, modelo ou versão |
| Price | Preço mín. (€) · Preço máx. (€) · placeholders "Mín." / "Máx." |
| Year | Ano mínimo · options: "Qualquer", then "Desde {ano}" for each year from the newest to the oldest year present in the published data |
| Mileage | Quilómetros máx. · options: Qualquer, 25 000 km, 50 000 km, 75 000 km, 100 000 km, 150 000 km, 200 000 km |
| Fuel / transmission | Combustível · Caixa · option "Qualquer" |
| Sort | Ordenar por |
| Active filter chip | e.g. "Gasóleo", "Até 20 000 €", "Desde 10 000 €", "Desde 2018", "Até 100 000 km", "Pesquisa: golf" · aria-label "Remover filtro: {texto}" |
| Clear | Limpar filtros (desktop/chips) · Limpar (sheet) |
| Sheet apply | Ver {n} resultados (Ver 1 resultado) · close aria-label "Fechar" |
| Pagination | Anterior · Seguinte · Página {x} de {y} · A mostrar {a}–{b} de {n} viaturas · nav aria-label "Paginação" |
| Card | price / "Preço sob consulta" · image alt "{Marca} {Modelo} {Ano}" |
| Detail back link | Voltar às viaturas |
| Gallery | Foto anterior · Foto seguinte · "{i} / {n}" · thumb aria-label "Ver foto {i} de {n}" · lightbox close "Fechar" · no photo: "Sem fotografias" |
| Key facts labels | Ano · Quilómetros · Combustível · Caixa |
| Sections | Especificações · Equipamento · Descrição |
| Spec labels | Marca · Modelo · Versão · Ano · Quilómetros · Combustível · Caixa · Cilindrada · Potência · Carroçaria · Tração · Cor exterior · Cor interior · Portas · Lugares · Emissões |
| Reference | Referência: {id} |
| Contact (fixed, everywhere) | **Contacto pelo Messenger** |
| Footer | Contacto pelo Messenger · Aviso de privacidade · © {ano} SilvaStock |
| No results | Nenhuma viatura encontrada · Experimente alargar o preço ou remover alguns filtros. · [Limpar filtros] |
| Empty catalogue | Ainda não há viaturas publicadas · Volte em breve para ver as novidades. |
| Vehicle 404 | Esta viatura já não está disponível · Pode ter sido vendida ou retirada do site. · [Ver viaturas disponíveis] |
| Generic 404 | Página não encontrada · O endereço pode estar errado ou a página foi removida. · [Ir para as viaturas] |
| Privacy page H1 | Aviso de privacidade (body text supplied by the owner — Phase 5) |

## 4. Back office

| Where | Text |
|---|---|
| Login H1 / text | Entrar no back office · Acesso reservado aos administradores do stand. |
| Login button | Entrar com Google |
| Login note | Ao entrar, é usada a sua conta Google. Não guardamos a sua palavra-passe. |
| `?error=not_allowed` | **Não foi possível entrar.** Esta conta Google não tem acesso ao back office. Se acha que é um erro, fale com o responsável do stand. |
| 429 | **Demasiadas tentativas.** Aguarde alguns minutos e tente novamente. |
| 401 (session expired, redirected) | **A sua sessão terminou.** Entre novamente para continuar. |
| Shell | SilvaStock · Back office · Painel · Inventário · Nova viatura · Terminar sessão · menu "Abrir menu" / "Fechar menu" · nav aria-label "Principal" |
| Dashboard | Painel · Olá, {primeiro nome}. · Total · Rascunho · Publicadas · Vendidas · Ver inventário / Ver rascunhos / Ver publicadas / Ver vendidas · Rascunhos recentes · Ver todos · "Atualizado {data relativa}" |
| Inventory | Inventário · {n} viaturas · + Nova viatura · Pesquisar · placeholder "Marca, modelo, matrícula ou VIN" · tabs Todas / Rascunho / Publicadas / Vendidas (with counts) · Ordenar · columns Viatura, Matrícula, Ano, Km, Preço, Estado, Fotos, Atualizado · row menu "Ações para {viatura}" |
| Row menu (by status) | Draft: Editar · Publicar · Eliminar — Published: Editar · Despublicar · Marcar como vendida · Ver no site — Sold: Editar · Reabrir como rascunho |
| Inventory empty / error | Ainda não há viaturas · Crie a primeira viatura para começar. · [Nova viatura] — Não foi possível carregar o inventário · Verifique a ligação e tente novamente. Se o servidor estava em pausa, pode demorar alguns segundos a responder. · [Tentar novamente] |
| Inventory no match | Nenhuma viatura corresponde à pesquisa. · [Limpar pesquisa] |
| Vehicle page | ← Inventário · H1 "{Marca} {Modelo}" (new: "Nova viatura") · "N.º {id} · Atualizado a {data-hora} por {email}" |
| Status actions | Publicar · Despublicar · Marcar como vendida · Reabrir como rascunho · Eliminar · Ver no site |
| Sections | Dados gerais · Especificações · Equipamento · Fotos · Documentos · Dados internos · overline "NESTA PÁGINA" |
| Section help | Dados gerais: "Marca e modelo bastam para guardar como rascunho. Para publicar: ano, quilómetros, combustível, caixa, preço (ou sob consulta) e foto principal." · Especificações: "Opcionais. Só os campos preenchidos aparecem no site." · Equipamento: "Enter adiciona. Máx. 100 itens, até 100 caracteres cada. A ordem é a do site." · Dados internos: "Visíveis apenas no back office. Nunca aparecem no site." |
| Field labels | Marca* · Modelo* · Versão · Ano · Quilómetros · Preço (€) · Preço sob consulta (oculta o preço no site) · Combustível · Caixa · Descrição · Cilindrada (cm³) · Potência (kW) · Carroçaria · Tração · Cor exterior · Cor interior · Portas · Lugares · Emissões · Adicionar equipamento · Matrícula · VIN · Notas internas |
| Helpers | Quilómetros: "Em km." · Potência: "≈ {cv} cv" (live) · Emissões: "Ex.: Euro 6d ou 120 g/km" · Matrícula: "Máx. 16 caracteres." · VIN: "17 caracteres, sem I, O ou Q." · Descrição: "Opcional. As quebras de linha são mantidas no site." · counters "{n} / 5000" |
| Client validation | Indique a marca. · Indique o modelo. · Indique o ano. · O ano deve estar entre 1900 e {ano atual + 1}. · Indique os quilómetros. · Indique um preço ou selecione "Preço sob consulta". · Selecione o combustível. · Selecione a caixa. · O VIN tem de ter 17 caracteres (sem I, O ou Q). · Máximo de {n} caracteres. · Este equipamento já está na lista. |
| Readiness banner (Draft) | **Faltam dados para publicar.** {lista de campos, cada um ligação} |
| 422 banner | **O veículo não pode ser publicado.** Corrija os campos assinalados: {lista}. |
| 409 toast | Esta ação já não é possível: o estado da viatura mudou. A página foi atualizada. |
| Save bar | Alterações por guardar · Descartar · Guardar (loading "A guardar…") |
| Leave with unsaved changes | Dialog: **Sair sem guardar?** As alterações feitas nesta página vão perder-se. [Continuar a editar] [Sair sem guardar] |
| Success toasts | Viatura criada. · Alterações guardadas. · Viatura publicada. O site atualiza em 1–3 minutos. · Viatura despublicada. O site atualiza em 1–3 minutos. · Viatura marcada como vendida. O site atualiza em 1–3 minutos. · Viatura reaberta como rascunho. · Viatura eliminada. · Fotos enviadas. · Ordem das fotos guardada. · Foto principal alterada. · Foto eliminada. · Documento enviado. · Documento eliminado. |
| Error toasts | Não foi possível guardar. Tente novamente. · Não foi possível concluir a ação. Tente novamente. · A sua sessão terminou. Entre novamente. · Não tem permissão para esta ação. |
| Confirm: publish | not confirmed (publishing is reversible) |
| Confirm: unpublish | **Despublicar viatura?** A viatura deixa de aparecer no site após a atualização (1–3 min). Pode voltar a publicá-la. [Cancelar] [Despublicar] |
| Confirm: sold | **Marcar como vendida?** A viatura deixa de aparecer no site após a atualização (1–3 min). Fica no inventário como Vendida. [Cancelar] [Marcar como vendida] |
| Reopen (Sold → Draft) | no confirmation (DECISIONS §7) — action + toast only |
| Confirm: delete vehicle | **Eliminar viatura?** Vai eliminar "{Marca} {Modelo} {Ano}", com as fotos e os documentos. Esta ação não pode ser anulada. [Cancelar] [Eliminar] |
| Confirm: delete photo | **Eliminar foto?** Esta ação não pode ser anulada. [Cancelar] [Eliminar] |
| Confirm: delete document | **Eliminar documento?** Vai eliminar "{nome}". Esta ação não pode ser anulada. [Cancelar] [Eliminar] |
| Photos | Fotos · {n} de 30 · Arraste fotos para aqui ou [Escolher ficheiros] · "JPEG, PNG ou WebP · máx. 10 MB por foto · até 30 fotos" · drag-over "Largue as fotos para as enviar" · "A enviar {i} de {n} fotos…" · Principal · Tornar principal · Mover para a esquerda · Mover para a direita · handle aria-label "Reordenar foto {i}" · live region "Foto {i} movida para a posição {j}." · empty "Ainda não há fotos. A primeira foto enviada fica como principal." · new vehicle: "Guarde a viatura para poder adicionar fotos." |
| Photo errors (client) | "{ficheiro}" não é um formato aceite (JPEG, PNG ou WebP). · "{ficheiro}" tem mais de 10 MB. · Pode enviar no máximo {restantes} fotos (limite de 30). · Nenhuma foto foi enviada. (server is all-or-nothing) |
| Documents | Documentos · "Privados: nunca aparecem no site." · Categoria · Nome (opcional) · placeholder "Se ficar vazio, usa o nome do ficheiro" · [Escolher ficheiro] · "PDF, JPEG ou PNG · máx. 15 MB" · [Enviar documento] · Transferir · empty "Ainda não há documentos." · error "Não foi possível transferir o documento. Tente novamente." |
| Document errors (client) | "{ficheiro}" não é um formato aceite (PDF, JPEG ou PNG). · "{ficheiro}" tem mais de 15 MB. · Selecione a categoria. |
| Generic | Cancelar · Fechar · Tentar novamente · A carregar… · Obrigatório |
