let deleteBtns = document.querySelectorAll('.delete-btn');
deleteBtns.forEach(delBtn => {

    delBtn.addEventListener('click', () => {
        let confirmation = window.confirm("Potvrdite brisanje statusa!");

        if (!confirmation) return false;

        let id = delBtn.getAttribute('data-id');

        fetch('/Zadaci/DeleteStatus/' + id, {
            method: 'DELETE'
        }).then(res => {
            console.log(res);
            location.reload();
        }).catch(err => console.log(err));

    });
});