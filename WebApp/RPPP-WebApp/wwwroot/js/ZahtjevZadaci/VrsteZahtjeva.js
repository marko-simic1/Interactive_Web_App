let deleteBtns = document.querySelectorAll('.delete-btn');
deleteBtns.forEach(delBtn => {

    delBtn.addEventListener('click', () => {
        let confirmation = window.confirm("Potvrdite brisanje vrste zahtjeva!");

        if (!confirmation) return false;

        let id = delBtn.getAttribute('data-id');

        fetch('/Zahtjev/DeleteVrstuZahtjeva/' + id, {
            method: 'DELETE'
        }).then(res => {
            console.log(res);
            location.reload();
        }).catch(err => console.log(err));

    });
});