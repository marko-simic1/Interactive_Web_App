document.addEventListener('DOMContentLoaded', function () {
    var spans = document.querySelectorAll('.truncated-text');

    spans.forEach(function (span) {
        let expandSpan = span.querySelector('.expand-button');
        let truncateSpan = span.querySelector('.truncate-button');
        let spanText = span.querySelector('.textOpis');

        expandSpan.addEventListener('click', function () {
            let fullText = span.getAttribute('data-full-text');
            spanText.innerHTML = fullText;

            expandSpan.classList.add("d-none");
            truncateSpan.classList.remove("d-none");
        });

        truncateSpan.addEventListener('click', function () {
            let truncatedText = span.getAttribute('data-full-text').substring(0, 50);
            spanText.innerHTML = truncatedText;

            truncateSpan.classList.add("d-none");
            expandSpan.classList.remove("d-none");
        });
    });
});


let deleteBtns = document.querySelectorAll('.delete-btn');
deleteBtns.forEach(delBtn => {

    delBtn.addEventListener('click', () => {
        let confirmation = window.confirm("Sigurno želite obrisati zadatak?");

        if (!confirmation) return false;

        let id = delBtn.getAttribute('data-id');

        fetch('/Zadaci/DeleteZadatak/' + id, {
            method: 'DELETE'
        }).then(res => {
            console.log(res);
            location.reload();
        }).catch(err => console.log(err));

    });
});