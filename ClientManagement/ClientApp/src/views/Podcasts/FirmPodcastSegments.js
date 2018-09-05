import React, { Component } from 'react';
import { Card, CardHeader, CardBody } from 'reactstrap';
import BootstrapTable from 'react-bootstrap-table-next';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Link } from 'react-router-dom';

class FirmPodcastSegments extends Component {
  constructor(props) {
    super(props);

    this.state = {
      segments: [],
      columns: [{
        dataField: 'id',
        text: 'Id',
        hidden: true,
        headerStyle: (colum, colIndex) => {
          return { width: '50px' };
        }
      },
      {
        dataField: 'title',
        text: 'Title',
        sort: true,
        headerStyle: (colum, colIndex) => {
          return { width: '250px' };
        },
        formatter: this.renderSegmentEditLink
      },
      {
          dataField: 'description',
          text: 'Description'
      },
      {
          dataField: 'startsOn',
          text: 'Starts',
          sort: true
      },
      {
          dataField: 'endsOn',
          text: 'Ends',
          sort: true
      },
      {
          dataField: 'segmentId',
          text: 'Segment',
          formatter: this.renderSegmentListenLink,
          align: 'center',
          headerStyle: (colum, colIndex) => {
            return { width: '330px' };
          }
      },
      {
        dataField: 'segmentURL',
        hidden: true
      }
      ]
    };
  }

  componentDidMount() {
    axios.get('/api/firmpodcastsegments', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ segments: response.data });
        }
      })
      .catch(error => {
        alert(error);
        window.location.replace('/');
      });
  }

  renderSegmentEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={`/podcasts/firmpodcastsegments/${row.id}`}>{cell}</Link>
      </span>
    );
  }

  renderSegmentListenLink(cell, row, rowIndex) {
    var link = row.segmentURL;
    return (
      <audio controls>
        <source src={link} />
      </audio>
    );
  }


  render() {

    return (
      <div className="animated">
        <Card>
          <CardHeader>
            <i className="icon-menu" />Firm Podcast Segments{' '}
            <div className="card-header-actions">
              <Link className="card-header-action" to={`/podcasts/firmpodcastsegments/0`}><small className="text-muted">Add New</small></Link>
            </div>
          </CardHeader>
          <CardBody>
            <BootstrapTable striped hover keyField='id' data={this.state.segments} columns={this.state.columns} noDataIndication="No podcast segments for firm have been added" />
          </CardBody>
        </Card>
      </div>
    );
  }



}

export default FirmPodcastSegments;
